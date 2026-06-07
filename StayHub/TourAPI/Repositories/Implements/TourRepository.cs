using Microsoft.EntityFrameworkCore;
using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Repositories.Implements
{
    public class TourRepository : ITourRepository
    {
        private readonly StayHubCatalogDbContext _context;

        public TourRepository(StayHubCatalogDbContext context)
        {
            _context = context;
        }
        public async Task Add(Tour model)
        {
            await _context.Tours.AddAsync(model);
        }

        public async Task Delete(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.Reviews)
                .Include(t => t.TourItineraries)
                .Include(t => t.TourSchedules)
                .Include(t => t.Wishlists)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
                return;
            _context.Reviews.RemoveRange(tour.Reviews);

            _context.TourItineraries.RemoveRange(tour.TourItineraries);

            _context.TourSchedules.RemoveRange(tour.TourSchedules);

            _context.Wishlists.RemoveRange(tour.Wishlists);

            // remove parent
            _context.Tours.Remove(tour);

            await _context.SaveChangesAsync();
        }

        public async Task<TourPageResult> GetPagedAsync(TourQueryOptions options)
        {
            var page = Math.Max(1, options.Page);
            var pageSize = Math.Clamp(options.PageSize, 1, 1000);
            IQueryable<Tour> query = _context.Tours.AsNoTracking();

            if (options.ActiveOnly)
            {
                query = query.Where(t => t.Status == "Active");
            }

            if (!string.IsNullOrWhiteSpace(options.SearchTerm))
            {
                var term = options.SearchTerm.Trim();
                query = query.Where(t =>
                    t.Name.Contains(term) ||
                    (t.Description != null && t.Description.Contains(term)) ||
                    t.TourItineraries.Any(i =>
                        (i.Title != null && i.Title.Contains(term)) ||
                        (i.Description != null && i.Description.Contains(term))));
            }

            if (options.CategoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == options.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(options.Country))
            {
                var country = options.Country.Trim();
                query = query.Where(t => t.Country == country);
            }

            if (!string.IsNullOrWhiteSpace(options.City))
            {
                var city = options.City.Trim();
                query = query.Where(t => t.City == city);
            }

            if (options.CreatedBy.HasValue)
            {
                query = query.Where(t => t.CreatedBy == options.CreatedBy.Value);
            }

            if (options.MinPrice.HasValue || options.MaxPrice.HasValue)
            {
                query = query.Where(t => t.TourSchedules.Any(s =>
                    s.TourScheduleTickets.Any(ticket =>
                        (!options.MinPrice.HasValue || ticket.Price >= options.MinPrice.Value) &&
                        (!options.MaxPrice.HasValue || ticket.Price <= options.MaxPrice.Value))));
            }

            if (options.StartDate.HasValue || options.EndDate.HasValue)
            {
                var startDate = options.StartDate?.Date;
                var endDateExclusive = options.EndDate?.Date.AddDays(1);
                query = query.Where(t => t.TourSchedules.Any(s =>
                    (!startDate.HasValue || s.DepartureDate >= startDate.Value) &&
                    (!endDateExclusive.HasValue || s.ReturnDate < endDateExclusive.Value)));
            }

            if (options.Duration.HasValue)
            {
                var duration = options.Duration.Value;
                query = query.Where(t =>
                    t.TourItineraries.Count == duration ||
                    t.TourSchedules.Any(s =>
                        EF.Functions.DateDiffDay(s.DepartureDate, s.ReturnDate) == duration ||
                        EF.Functions.DateDiffDay(s.DepartureDate, s.ReturnDate) + 1 == duration));
            }

            var total = await query.CountAsync();

            query = options.SortBy?.Trim().ToLowerInvariant() switch
            {
                "price_asc" => query
                    .OrderBy(t => t.TourSchedules
                        .SelectMany(s => s.TourScheduleTickets)
                        .Select(ticket => (long?)ticket.Price)
                        .Min() ?? long.MaxValue)
                    .ThenBy(t => t.Id),
                "price_desc" => query
                    .OrderByDescending(t => t.TourSchedules
                        .SelectMany(s => s.TourScheduleTickets)
                        .Select(ticket => (long?)ticket.Price)
                        .Min() ?? 0)
                    .ThenBy(t => t.Id),
                "date_asc" => query
                    .OrderBy(t => t.TourSchedules
                        .Select(s => (DateTime?)s.DepartureDate)
                        .Min() ?? DateTime.MaxValue)
                    .ThenBy(t => t.Id),
                "date_desc" => query
                    .OrderByDescending(t => t.TourSchedules
                        .Select(s => (DateTime?)s.DepartureDate)
                        .Min() ?? DateTime.MinValue)
                    .ThenBy(t => t.Id),
                _ when options.SortDescendingById => query.OrderByDescending(t => t.Id),
                _ => query.OrderBy(t => t.Id)
            };

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(t => t.TourItineraries)
                .Include(t => t.TourSchedules)
                    .ThenInclude(t => t.TourScheduleTickets)
                .Include(t => t.Reviews)
                    .ThenInclude(r => r.ReviewReplies)
                .AsSplitQuery()
                .ToListAsync();

            return new TourPageResult
            {
                Items = items,
                Total = total
            };
        }

        public async Task<Tour> GetById(int id)
        {
            try
            {
                return await _context.Tours
                    .Include(t => t.TourItineraries.OrderBy(x => x.DayNumber))
                    .Include(t => t.TourSchedules)
                        .ThenInclude(t => t.TourScheduleTickets)
                    .Include(t => t.Reviews)
                        .ThenInclude(r => r.ReviewReplies)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(g => g.Id == id);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

        }

        public void Update(Tour model)
        {
            _context.Tours.Update(model);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountByCategoryIdAsync(int categoryId)
        {
            try
            {
                // Đếm số lượng tour có CategoryId truyền vào
                return await _context.Tours.CountAsync(t => t.CategoryId == categoryId);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
