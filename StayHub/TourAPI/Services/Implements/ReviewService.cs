using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public ReviewService(
            IReviewRepository reviewRepository,
            IReviewReplyRepository reviewReplyRepository,
            ITourRepository tourRepository,
            ITourScheduleRepository tourScheduleRepository,
            IOrderService orderService,
            INotificationInternalService notificationInternalService,
            IMapper mapper)
        {
            _reviewRepository = reviewRepository;
            _reviewReplyRepository = reviewReplyRepository;
            _tourRepository = tourRepository;
            _tourScheduleRepository = tourScheduleRepository;
            _orderService = orderService;
            _notificationInternalService = notificationInternalService;
            _mapper = mapper;
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

            var schedules = await _tourScheduleRepository.GetByTourIdAsync(model.TourId);
            if (schedules == null || !schedules.Any())
                throw new Exception("This tour has no schedules.");

            var completedSchedules = schedules.Where(s => s.ReturnDate < DateTime.UtcNow).ToList();
            if (!completedSchedules.Any())
                throw new Exception("This tour schedule has not ended yet.");

            var completedScheduleIds = completedSchedules.Select(s => s.Id).ToList();

            var completedBooking = await _orderService.CheckCompletedOrder(new CheckCompletedBookingRequest
            {
                CustomerId = customerId,
                ScheduleIds = completedScheduleIds
            });

            if (!completedBooking)
                throw new Exception("You can only review trips you have completed and checked in for.");

            var review = _mapper.Map<Review>(model);
            review.CreatedAt = DateTime.UtcNow;
            review.UpdatedAt = DateTime.UtcNow;
            review.IsHidden = false;

            await _reviewRepository.AddAsync(review);
            await _reviewRepository.SaveChangesAsync();

            return _mapper.Map<ReadReviewDTO>(review);
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

            return _mapper.Map<ReadReviewDTO>(review);
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
                $"Manager/Staff vừa trả lời review của bạn: {model.Content}"
            );

            return _mapper.Map<ReadReviewReplyDTO>(reply);
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

            return _mapper.Map<ReadReviewReplyDTO>(reply);
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
            return review == null ? null : _mapper.Map<ReadReviewDTO>(review);
        }

        public async Task<IEnumerable<ReadReviewDTO>> GetMyReviewsAsync(int customerId)
        {
            var reviews = await _reviewRepository.GetByCustomerAsync(customerId);
            return _mapper.Map<IEnumerable<ReadReviewDTO>>(reviews);
        }

        public async Task<IEnumerable<ReadReviewDTO>> GetReviewsByTourAsync(int tourId, bool includeHidden = false)
        {
            var reviews = await _reviewRepository.GetByTourAsync(tourId, includeHidden);

            return _mapper.Map<IEnumerable<ReadReviewDTO>>(reviews);
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
    }
}
