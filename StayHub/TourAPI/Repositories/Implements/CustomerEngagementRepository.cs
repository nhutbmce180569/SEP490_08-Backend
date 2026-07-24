using Microsoft.EntityFrameworkCore;
using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Repositories.Implements
{
    public class CustomerEngagementRepository : ICustomerEngagementRepository
    {
        private readonly StayHubCatalogDbContext _context;

        public CustomerEngagementRepository(StayHubCatalogDbContext context)
        {
            _context = context;
        }

        public async Task<CustomerEngagementAnalyticsDTO> GetEngagementOverviewAsync(int totalCustomers)
        {
            var reviews = await _context.Reviews.AsNoTracking().Include(r => r.Tour).ToListAsync();
            var wishlists = await _context.Wishlists.AsNoTracking().Include(w => w.Tour).ToListAsync();

            var visibleReviews = reviews.Where(r => !r.IsHidden).ToList();
            var uniqueReviewers = reviews.Select(r => r.CustomerId).Distinct().Count();
            var uniqueWishlistCustomers = wishlists.Select(w => w.CustomerId).Distinct().Count();

            var ratingDistribution = reviews
                .GroupBy(r => r.Rating)
                .Select(g => new AnalyticsLabelCountDTO
                {
                    Label = $"{g.Key} Star",
                    Count = g.Count(),
                    Percentage = reviews.Count > 0
                        ? Math.Round(g.Count() * 100m / reviews.Count, 2)
                        : 0
                })
                .OrderBy(x => x.Label)
                .ToList();

            var topWishlisted = wishlists
                .GroupBy(w => w.TourId)
                .Select(g => new TopTourEngagementDTO
                {
                    TourId = g.Key,
                    TourName = g.First().Tour?.Name,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var topReviewed = reviews
                .GroupBy(r => r.TourId)
                .Select(g => new TopTourEngagementDTO
                {
                    TourId = g.Key,
                    TourName = g.First().Tour?.Name,
                    Count = g.Count(),
                    AverageRating = Math.Round(g.Average(r => (decimal)r.Rating), 2)
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            return new CustomerEngagementAnalyticsDTO
            {
                TotalReviews = reviews.Count,
                VisibleReviews = visibleReviews.Count,
                HiddenReviews = reviews.Count - visibleReviews.Count,
                UniqueReviewers = uniqueReviewers,
                AverageRating = reviews.Count > 0
                    ? Math.Round(reviews.Average(r => (decimal)r.Rating), 2)
                    : 0,
                RatingDistribution = ratingDistribution,
                TotalWishlists = wishlists.Count,
                UniqueWishlistCustomers = uniqueWishlistCustomers,
                AverageWishlistsPerCustomer = uniqueWishlistCustomers > 0
                    ? Math.Round(wishlists.Count / (decimal)uniqueWishlistCustomers, 2)
                    : 0,
                TopWishlistedTours = topWishlisted,
                TopReviewedTours = topReviewed,
                ReviewParticipationRate = totalCustomers > 0
                    ? Math.Round(uniqueReviewers * 100m / totalCustomers, 2)
                    : 0
            };
        }

        public async Task<Dictionary<int, int>> GetReviewCountsByCustomerAsync()
        {
            return await _context.Reviews
                .AsNoTracking()
                .GroupBy(r => r.CustomerId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
        }

        public async Task<Dictionary<int, int>> GetWishlistCountsByCustomerAsync()
        {
            return await _context.Wishlists
                .AsNoTracking()
                .GroupBy(w => w.CustomerId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
        }

        public async Task<Dictionary<int, decimal>> GetAverageRatingsByCustomerAsync()
        {
            return await _context.Reviews
                .AsNoTracking()
                .GroupBy(r => r.CustomerId)
                .Select(g => new { g.Key, Avg = g.Average(r => (decimal)r.Rating) })
                .ToDictionaryAsync(x => x.Key, x => Math.Round(x.Avg, 2));
        }
    }
}
