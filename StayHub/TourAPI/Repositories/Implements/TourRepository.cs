using Microsoft.EntityFrameworkCore;
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

        public async Task<(List<Tour> Tours, int Total)> GetAll(
            int page,
            int pageSize,
            string? searchTerm = null,
            int? categoryId = null,
            int? createdBy = null)
        {
            var query = _context.Tours.AsNoTracking();

            query = ApplySearch(query, searchTerm);

            if (categoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == categoryId.Value);
            }

            if (createdBy.HasValue)
            {
                query = query.Where(t => t.CreatedBy == createdBy.Value);
            }

            return await GetPagedTours(query.OrderBy(t => t.Id), page, pageSize);
        }

        public async Task<(List<Tour> Tours, int Total)> GetActiveTours(
            int page,
            int pageSize)
        {
            var query = _context.Tours
                .AsNoTracking()
                .Where(t => t.Status == "Active")
                .OrderBy(t => t.Id);

            return await GetPagedTours(query, page, pageSize);
        }

        public async Task<(List<Tour> Tours, int Total)> SearchTours(
            int page,
            int pageSize,
            string? searchTerm = null,
            int? categoryId = null,
            string? country = null,
            string? city = null,
            long? minPrice = null,
            long? maxPrice = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? duration = null,
            string? sortBy = null)
        {
            var query = _context.Tours
                .AsNoTracking()
                .Where(t => t.Status == "Active");

            query = ApplySearch(query, searchTerm);

            if (categoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(country))
            {
                var normalizedCountry = country.Trim();
                query = query.Where(t => t.Country == normalizedCountry);
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                var normalizedCity = city.Trim();
                query = query.Where(t => t.City == normalizedCity);
            }

            if (minPrice.HasValue || maxPrice.HasValue)
            {
                query = query.Where(t => t.TourSchedules.Any(s =>
                    s.TourScheduleTickets.Any(ticket =>
                        (!minPrice.HasValue || ticket.Price >= minPrice.Value) &&
                        (!maxPrice.HasValue || ticket.Price <= maxPrice.Value))));
            }

            if (startDate.HasValue || endDate.HasValue)
            {
                var start = startDate?.Date;
                var endExclusive = endDate?.Date.AddDays(1);
                query = query.Where(t => t.TourSchedules.Any(s =>
                    (!start.HasValue || s.DepartureDate >= start.Value) &&
                    (!endExclusive.HasValue || s.ReturnDate < endExclusive.Value)));
            }

            if (duration.HasValue)
            {
                var numberOfDays = duration.Value;
                query = query.Where(t =>
                    t.TourItineraries.Count == numberOfDays ||
                    t.TourSchedules.Any(s =>
                        EF.Functions.DateDiffDay(s.DepartureDate, s.ReturnDate) == numberOfDays ||
                        EF.Functions.DateDiffDay(s.DepartureDate, s.ReturnDate) + 1 == numberOfDays));
            }

            var sortedQuery = sortBy?.Trim().ToLowerInvariant() switch
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
                _ => query.OrderBy(t => t.Id)
            };

            return await GetPagedTours(sortedQuery, page, pageSize);
        }

        public async Task<(List<Tour> Tours, int Total)> GetByAdmin(
            int page,
            int pageSize,
            string? searchTerm = null,
            int? managerId = null)
        {
            var query = _context.Tours.AsNoTracking();

            if (managerId.HasValue)
            {
                query = query.Where(t => t.CreatedBy == managerId.Value);
            }

            query = ApplySearch(query, searchTerm)
                .OrderByDescending(t => t.Id);

            return await GetPagedTours(query, page, pageSize);
        }

        public async Task<(List<Tour> Tours, int Total)> GetByManager(
            int managerId,
            int page,
            int pageSize,
            string? searchTerm = null)
        {
            var query = _context.Tours
                .AsNoTracking()
                .Where(t => t.CreatedBy == managerId);

            query = ApplySearch(query, searchTerm);

            return await GetPagedTours(
                query.OrderByDescending(t => t.Id),
                page,
                pageSize);
        }

        private static IQueryable<Tour> ApplySearch(
            IQueryable<Tour> query,
            string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return query;
            }

            var term = searchTerm.Trim();

            return query.Where(t =>
                t.Name.Contains(term) ||
                (t.Description != null && t.Description.Contains(term)) ||
                t.TourItineraries.Any(i =>
                    (i.Title != null && i.Title.Contains(term)) ||
                    (i.Description != null && i.Description.Contains(term))));
        }

        private static async Task<(List<Tour> Tours, int Total)> GetPagedTours(
            IQueryable<Tour> query,
            int page,
            int pageSize)
        {
            var normalizedPage = Math.Max(1, page);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 1000);
            var total = await query.CountAsync();

            var tours = await query
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Include(t => t.TourItineraries)
                .Include(t => t.TourSchedules)
                    .ThenInclude(t => t.TourScheduleTickets)
                        .ThenInclude(t => t.Promotions)
                .Include(t => t.Reviews)
                    .ThenInclude(r => r.ReviewReplies)
                .AsSplitQuery()
                .ToListAsync();

            return (tours, total);
        }

        public async Task<Tour> GetById(int id)
        {
            try
            {
                return await _context.Tours
                    .Include(t => t.TourItineraries.OrderBy(x => x.DayNumber))
                    .Include(t => t.TourSchedules)
                        .ThenInclude(t => t.TourScheduleTickets)
                            .ThenInclude(t => t.Promotions)
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
