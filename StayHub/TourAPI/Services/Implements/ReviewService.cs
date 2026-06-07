using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Repositories;
using TourAPI.Services;

namespace TourAPI.Services.Implements
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IReviewReplyRepository _reviewReplyRepository;
        private readonly ITourRepository _tourRepository;
        private readonly ITourScheduleRepository _tourScheduleRepository;
        private readonly IOrderService _orderService;
        private readonly INotificationInternalService _notificationInternalService;
        private readonly IMapper _mapper;
        private readonly HttpClient _httpClient;
        private readonly string _gatewayUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReviewService(
            IReviewRepository reviewRepository,
            IReviewReplyRepository reviewReplyRepository,
            ITourRepository tourRepository,
            ITourScheduleRepository tourScheduleRepository,
            IOrderService orderService,
            INotificationInternalService notificationInternalService,
            IMapper mapper,
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _reviewRepository = reviewRepository;
            _reviewReplyRepository = reviewReplyRepository;
            _tourRepository = tourRepository;
            _tourScheduleRepository = tourScheduleRepository;
            _orderService = orderService;
            _notificationInternalService = notificationInternalService;
            _mapper = mapper;
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _gatewayUrl = configuration.GetValue<string>("GatewayApi:BaseUrl") ?? "https://localhost:7010";
            _httpClient.BaseAddress = new Uri(_gatewayUrl);
        }

        public async Task<ReadReviewDTO> CreateReviewAsync(CreateReviewDTO model, int customerId)
        {
            if (model.CustomerId != customerId)
                throw new Exception("Cannot create a review for another customer.");

            var tour = await _tourRepository.GetById(model.TourId);
            if (tour == null)
                throw new Exception("Tour not found.");

            var existingReview = await _reviewRepository.GetByTourAndCustomerAsync(model.TourId, customerId);
            if (existingReview != null)
                throw new Exception("You can only review this tour once.");

            // ✅ Lấy tất cả scheduleIds của tour này
            var schedules = await _tourScheduleRepository.GetByTourIdAsync(model.TourId);
            if (schedules == null || !schedules.Any())
                throw new Exception("This tour has no schedules.");

            var scheduleIds = schedules.Select(s => s.Id).ToList();

            var hasCompletedBooking = await _orderService.CheckCompletedOrder(new CheckCompletedBookingRequest
            {
                CustomerId = customerId,
                ScheduleIds = scheduleIds
            });

            if (!hasCompletedBooking)
                throw new Exception("You can only review trips you have paid and checkedin in for.");

            var review = _mapper.Map<Review>(model);
            review.CreatedAt = DateTime.UtcNow;
            review.UpdatedAt = DateTime.UtcNow;
            review.IsHidden = false;

            await _reviewRepository.AddAsync(review);
            await _reviewRepository.SaveChangesAsync();

            var result = _mapper.Map<ReadReviewDTO>(review);
            await PopulateReviewerNamesAsync(new[] { result });
            return result;
        }

        public async Task<ReadReviewDTO> UpdateReviewAsync(int reviewId, UpdateReviewDTO model, int customerId)
        {
            if (model.CustomerId != customerId)
                throw new Exception("Cannot update a review for another customer.");

            var review = await _reviewRepository.GetByIdAsync(reviewId);
            if (review == null)
                throw new Exception("Review not found.");

            if (review.CustomerId != customerId)
                throw new Exception("You can only update your own review.");

            if (!review.CreatedAt.HasValue)
                throw new Exception("Review creation date is missing.");

            if (review.CreatedAt.Value.AddDays(1) < DateTime.UtcNow)
                throw new Exception("You can only update a review within 1 day after it was created.");

            review.Rating = model.Rating;
            review.Comment = model.Comment;
            review.UpdatedAt = DateTime.UtcNow;

            _reviewRepository.Update(review);
            await _reviewRepository.SaveChangesAsync();

            var dto = _mapper.Map<ReadReviewDTO>(review);
            await PopulateReviewerNamesAsync(new[] { dto });
            return dto;
        }

        public async Task<ReadReviewReplyDTO> CreateReviewReplyAsync(int staffId, CreateReviewReplyDTO model)
        {
            var review = await _reviewRepository.GetByIdAsync(model.ReviewId);
            if (review == null)
                throw new Exception("Review not found.");

            var reply = new ReviewReply
            {
                ReviewId = model.ReviewId,
                UserId = staffId,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _reviewReplyRepository.AddAsync(reply);
            await _reviewReplyRepository.SaveChangesAsync();

            await _notificationInternalService.NotifyUserAsync(
                review.CustomerId,
                "Your review has a new reply",
                $"A Manager/Staff member has replied to your review: {model.Content}"
            );

            var dto = _mapper.Map<ReadReviewReplyDTO>(reply);
            return dto;
        }

        public async Task<ReadReviewReplyDTO> UpdateReviewReplyAsync(int replyId, int staffId, UpdateReviewReplyDTO model)
        {
            var reply = await _reviewReplyRepository.GetByIdAsync(replyId);
            if (reply == null)
                throw new Exception("Reply not found.");

            if (reply.UserId != staffId)
                throw new Exception("You can only update your own reply.");

            reply.Content = model.Content;
            reply.UpdatedAt = DateTime.UtcNow;

            _reviewReplyRepository.Update(reply);
            await _reviewReplyRepository.SaveChangesAsync();

            var dto = _mapper.Map<ReadReviewReplyDTO>(reply);
            return dto;
        }

        public async Task DeleteReviewReplyAsync(int replyId, int staffId)
        {
            var reply = await _reviewReplyRepository.GetByIdAsync(replyId);
            if (reply == null)
                throw new Exception("Reply not found.");

            if (reply.UserId != staffId)
                throw new Exception("You can only delete your own reply.");

            await _reviewReplyRepository.DeleteAsync(reply);
            await _reviewReplyRepository.SaveChangesAsync();
        }

        public async Task<ReadReviewDTO?> GetMyReviewByTourAsync(int tourId, int customerId)
        {
            var review = await _reviewRepository.GetByTourAndCustomerAsync(tourId, customerId);
            if (review == null)
                return null;

            var dto = _mapper.Map<ReadReviewDTO>(review);
            await PopulateReviewerNamesAsync(new[] { dto });
            return dto;
        }

        public async Task<IEnumerable<ReadReviewDTO>> GetMyReviewsAsync(int customerId)
        {
            var reviews = await _reviewRepository.GetByCustomerAsync(customerId);
            var dtos = _mapper.Map<IEnumerable<ReadReviewDTO>>(reviews);
            await PopulateReviewerNamesAsync(dtos);
            return dtos;
        }

        public async Task<PaginationDTO<ReadReviewDTO>> GetReviewsByTourAsync(int tourId, int page, int pageSize, int? rating, string sortOrder, bool includeHidden = false)
        {
            // Lấy câu query gốc
            var query = _reviewRepository.GetBaseQueryByTour(tourId, includeHidden);

            // 1. Áp dụng Lọc theo số sao
            if (rating.HasValue)
            {
                query = query.Where(r => r.Rating == rating.Value);
            }

            // 2. Áp dụng Sắp xếp
            if (sortOrder?.ToLower() == "oldest")
            {
                query = query.OrderBy(r => r.CreatedAt);
            }
            else
            {
                query = query.OrderByDescending(r => r.CreatedAt); // Mặc định là mới nhất
            }

            // 3. Đếm tổng số lượng
            var totalCount = await query.CountAsync();

            // 4. Áp dụng Phân trang (Skip, Take)
            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 5. Map sang DTO và gán thông tin Avatar/Tên
            var dtos = _mapper.Map<List<ReadReviewDTO>>(reviews);
            await PopulateReviewerNamesAsync(dtos);

            return new PaginationDTO<ReadReviewDTO>
            {
                Data = dtos,
                CurrentPage = page,
                PageSize = pageSize,
                Total = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<PaginationDTO<ReadReviewDTO>> GetReviewsByManagerAsync( int managerId, int tourId, int page, int pageSize, int? rating, string sortOrder)
        {
            var query = _reviewRepository.GetBaseQueryByManager(managerId, includeHidden: true);

            if (tourId > 0)
                query = query.Where(r => r.TourId == tourId);

            if (rating.HasValue)
                query = query.Where(r => r.Rating == rating.Value);

            query = sortOrder?.ToLower() == "oldest"
                ? query.OrderBy(r => r.CreatedAt)
                : query.OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync();

            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<ReadReviewDTO>>(reviews);
            await PopulateReviewerNamesAsync(dtos);

            return new PaginationDTO<ReadReviewDTO>
            {
                Data = dtos,
                CurrentPage = page,
                PageSize = pageSize,
                Total = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task HideReviewAsync(int reviewId, bool hidden)
        {
            var review = await _reviewRepository.GetByIdAsync(reviewId);
            if (review == null)
                throw new Exception("Review not found.");

            review.IsHidden = hidden;
            review.UpdatedAt = DateTime.UtcNow;
            _reviewRepository.Update(review);
            await _reviewRepository.SaveChangesAsync();
        }



        private async Task PopulateReviewerNamesAsync(IEnumerable<ReadReviewDTO> reviews)
        {
            if (reviews == null || !reviews.Any()) return;

            var reviewList = reviews.ToList();

            var allUserIds = reviewList.Select(x => x.CustomerId)
                .Concat(reviewList.SelectMany(r => r.Replies?.Select(rep => rep.UserId) ?? Enumerable.Empty<int>()))
                .Distinct()
                .ToList();

            if (!allUserIds.Any()) return;

            var userNames = new Dictionary<int, (string Name, string? Avatar)>();

            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/users/batch/public", allUserIds);

                if (response.IsSuccessStatusCode)
                {
                    var apiResult = await response.Content.ReadFromJsonAsync<UserBatchApiResponse>();
                    if (apiResult?.Data != null)
                    {
                        foreach (var user in apiResult.Data)
                        {
                            userNames[user.Id] = (user.FullName ?? "Unknown User", user.AvatarUrl);
                        }
                    }
                }
            }
            catch
            {
            }

            foreach (var review in reviewList)
            {
                if (userNames.TryGetValue(review.CustomerId, out var userInfo))
                {
                    review.CustomerName = userInfo.Name;
                    review.CustomerAvatar = userInfo.Avatar;
                }

                if (review.Replies != null && review.Replies.Any())
                {
                    foreach (var rep in review.Replies)
                    {
                        if (userNames.TryGetValue(rep.UserId, out var replyUserInfo))
                        {
                            rep.UserName = replyUserInfo.Name;
                            rep.UserAvatar = replyUserInfo.Avatar;
                        }
                    }
                }
            }
        }


    }
}
