using AutoMapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Repositories;

namespace TourAPI.Services.Implements
{
    public class WishlistService : IWishlistService
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly ITourRepository _tourRepository;
        private readonly IMapper _mapper;

        public WishlistService(
            IWishlistRepository wishlistRepository,
            ITourRepository tourRepository,
            IMapper mapper)
        {
            _wishlistRepository = wishlistRepository;
            _tourRepository = tourRepository;
            _mapper = mapper;
        }

        public async Task<ReadWishlistItemDTO> AddToWishlistAsync(int tourId, int customerId)
        {
            if (tourId <= 0)
                throw new Exception("Invalid tour ID.");

            var tour = await _tourRepository.GetById(tourId);
            if (tour == null)
                throw new Exception("Tour not found.");

            if (!string.Equals(tour.Status, "Active", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Only active tours can be added to the wishlist.");

            var existing = await _wishlistRepository.GetByCustomerAndTourAsync(customerId, tourId);
            if (existing != null)
                throw new Exception("This tour is already in your wishlist.");

            var wishlist = new Wishlist
            {
                CustomerId = customerId,
                TourId = tourId
            };

            await _wishlistRepository.AddAsync(wishlist);
            await _wishlistRepository.SaveChangesAsync();

            var saved = await _wishlistRepository.GetByCustomerAndTourAsync(customerId, tourId);
            if (saved == null)
                throw new Exception("Failed to add tour to wishlist.");

            return _mapper.Map<ReadWishlistItemDTO>(saved);
        }

        public async Task RemoveFromWishlistAsync(int tourId, int customerId)
        {
            if (tourId <= 0)
                throw new Exception("Invalid tour ID.");

            var wishlist = await _wishlistRepository.GetByCustomerAndTourAsync(customerId, tourId);
            if (wishlist == null)
                throw new Exception("This tour is not in your wishlist.");

            _wishlistRepository.Remove(wishlist);
            await _wishlistRepository.SaveChangesAsync();
        }

        public async Task<List<ReadWishlistItemDTO>> GetMyWishlistAsync(int customerId)
        {
            var items = await _wishlistRepository.GetByCustomerAsync(customerId);
            return _mapper.Map<List<ReadWishlistItemDTO>>(items);
        }
    }
}
