using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Repositories;

namespace TourAPI.Services.Implements
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly ITourRepository _tourRepository;
        private readonly ITourScheduleRepository _tourScheduleRepository;
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;

        public ReviewService(
            IReviewRepository reviewRepository,
            ITourRepository tourRepository,
            ITourScheduleRepository tourScheduleRepository,
            IOrderService orderService,
            IMapper mapper)
        {
            _reviewRepository = reviewRepository;
            _tourRepository = tourRepository;
            _tourScheduleRepository = tourScheduleRepository;
            _orderService = orderService;
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

            // Lấy tất cả schedules của tour và kiểm tra xem có schedules đã kết thúc không
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

        public async Task<IEnumerable<ReadReviewDTO>> GetReviewsByTourAsync(int tourId)
        {
            var reviews = await _reviewRepository.GetByTourAsync(tourId);
            return _mapper.Map<IEnumerable<ReadReviewDTO>>(reviews);
        }
    }
}
