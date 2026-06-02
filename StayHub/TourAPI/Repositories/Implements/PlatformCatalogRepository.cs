using Microsoft.EntityFrameworkCore;
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Services;

namespace TourAPI.Repositories.Implements
{
    public class PlatformCatalogRepository : IPlatformCatalogRepository
    {
        private readonly StayHubCatalogDbContext _context;
        private readonly ICategoryService _categoryService;

        public PlatformCatalogRepository(
            StayHubCatalogDbContext context,
            ICategoryService categoryService)
        {
            _context = context;
            _categoryService = categoryService;
        }

        public async Task<PlatformCatalogAnalyticsDTO> GetCatalogStatsAsync(int topBookedTours)
        {
            var now = DateTime.UtcNow;
            var tours = await _context.Tours.AsNoTracking().ToListAsync();
            var categoryNames = await _categoryService.GetCategoryNamesAsync(
                tours.Select(t => t.CategoryId));
            var schedules = await _context.TourSchedules.AsNoTracking().Include(s => s.Tour).ToListAsync();
            var tickets = await _context.TourScheduleTickets.AsNoTracking().ToListAsync();

            var totalTours = tours.Count;
            var activeTours = tours.Count(t => t.Status == "Active");

            var upcoming = schedules.Count(s => s.DepartureDate > now);
            var ongoing = schedules.Count(s => s.DepartureDate <= now && s.ReturnDate >= now);
            var completed = schedules.Count(s => s.ReturnDate < now);

            var totalCapacity = tickets.Sum(t => t.Quantity);
            var totalSold = tickets.Sum(t => t.SoldQuantity ?? 0);
            var totalAvailable = tickets.Sum(t => t.AvailableQuantity);

            var soldByTour = tickets
                .Join(schedules, t => t.ScheduleId, s => s.Id, (t, s) => new { s.TourId, SoldQuantity = t.SoldQuantity ?? 0, TourName = s.Tour?.Name })
                .GroupBy(x => x.TourId)
                .Select(g => new TopTourEngagementDTO
                {
                    TourId = g.Key,
                    TourName = g.First().TourName,
                    Count = g.Sum(x => x.SoldQuantity)
                })
                .OrderByDescending(x => x.Count)
                .Take(topBookedTours)
                .ToList();

            return new PlatformCatalogAnalyticsDTO
            {
                TotalTours = totalTours,
                ActiveTours = activeTours,
                InactiveTours = totalTours - activeTours,
                TotalSchedules = schedules.Count,
                UpcomingSchedules = upcoming,
                OngoingSchedules = ongoing,
                CompletedSchedules = completed,
                TotalTicketCapacity = totalCapacity,
                TotalTicketsSold = totalSold,
                TotalTicketsAvailable = totalAvailable,
                ScheduleOccupancyRate = totalCapacity > 0
                    ? Math.Round(totalSold * 100m / totalCapacity, 2)
                    : 0,
                ToursByCategory = tours
                    .GroupBy(t => t.CategoryId)
                    .Select(g => new AnalyticsLabelCountDTO
                    {
                        Label = categoryNames.TryGetValue(g.Key, out var categoryName)
                            && !string.IsNullOrWhiteSpace(categoryName)
                                ? categoryName
                                : $"Category {g.Key}",
                        Count = g.Count(),
                        Percentage = totalTours > 0 ? Math.Round(g.Count() * 100m / totalTours, 2) : 0
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList(),
                ToursByCity = BuildLabelCounts(tours, totalTours, t => string.IsNullOrWhiteSpace(t.City) ? "Unknown" : t.City!),
                ToursByStatus = BuildLabelCounts(tours, totalTours, t => string.IsNullOrWhiteSpace(t.Status) ? "Unknown" : t.Status!),
                TopBookedTours = soldByTour
            };
        }

        public async Task<decimal> GetReviewResponseRateAsync()
        {
            var totalReviews = await _context.Reviews.AsNoTracking().CountAsync();
            if (totalReviews == 0) return 0;

            var reviewsWithReplies = await _context.ReviewReplies
                .AsNoTracking()
                .Select(r => r.ReviewId)
                .Distinct()
                .CountAsync();

            return Math.Round(reviewsWithReplies * 100m / totalReviews, 2);
        }

        private static List<AnalyticsLabelCountDTO> BuildLabelCounts<T>(
            List<T> items,
            int total,
            Func<T, string> labelSelector)
        {
            return items
                .GroupBy(labelSelector)
                .Select(g => new AnalyticsLabelCountDTO
                {
                    Label = g.Key,
                    Count = g.Count(),
                    Percentage = total > 0 ? Math.Round(g.Count() * 100m / total, 2) : 0
                })
                .OrderByDescending(x => x.Count)
                .ToList();
        }
    }
}
