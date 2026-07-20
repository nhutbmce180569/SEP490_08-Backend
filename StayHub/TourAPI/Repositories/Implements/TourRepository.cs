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
                normalizedCity = normalizedCity
                    .Replace("Thanh Pho ", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("Tinh ", "", StringComparison.OrdinalIgnoreCase)
                    .Replace(" City", "", StringComparison.OrdinalIgnoreCase)
                    .Replace(" Province", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();
                    
                query = query.Where(t => t.City != null && t.City.Contains(normalizedCity));
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

            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return await GetPagedTours(query.OrderBy(t => t.Id), page, pageSize);
            }

            var normalizedPage = Math.Max(1, page);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 1000);
            var total = await query.CountAsync();

            List<int> pagedIds;
            var sortMode = sortBy.Trim().ToLowerInvariant();

            if (sortMode == "price_asc")
            {
                var projections = await query.Select(t => new {
                    t.Id,
                    MinPrice = t.TourSchedules.SelectMany(s => s.TourScheduleTickets).Min(ticket => (long?)ticket.Price) ?? long.MaxValue
                }).ToListAsync();
                pagedIds = projections.OrderBy(x => x.MinPrice).ThenBy(x => x.Id).Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).Select(x => x.Id).ToList();
            }
            else if (sortMode == "price_desc")
            {
                var projections = await query.Select(t => new {
                    t.Id,
                    MinPrice = t.TourSchedules.SelectMany(s => s.TourScheduleTickets).Min(ticket => (long?)ticket.Price) ?? 0
                }).ToListAsync();
                pagedIds = projections.OrderByDescending(x => x.MinPrice).ThenBy(x => x.Id).Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).Select(x => x.Id).ToList();
            }
            else if (sortMode == "date_asc")
            {
                var projections = await query.Select(t => new {
                    t.Id,
                    MinDate = t.TourSchedules.Min(s => (DateTime?)s.DepartureDate) ?? DateTime.MaxValue
                }).ToListAsync();
                pagedIds = projections.OrderBy(x => x.MinDate).ThenBy(x => x.Id).Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).Select(x => x.Id).ToList();
            }
            else if (sortMode == "date_desc")
            {
                var projections = await query.Select(t => new {
                    t.Id,
                    MinDate = t.TourSchedules.Min(s => (DateTime?)s.DepartureDate) ?? DateTime.MinValue
                }).ToListAsync();
                pagedIds = projections.OrderByDescending(x => x.MinDate).ThenBy(x => x.Id).Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).Select(x => x.Id).ToList();
            }
            else
            {
                pagedIds = await query.OrderBy(t => t.Id).Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).Select(t => t.Id).ToListAsync();
            }

            var tours = await _context.Tours
                .Where(t => pagedIds.Contains(t.Id))
                .Include(t => t.TourItineraries)
                .Include(t => t.TourSchedules)
                    .ThenInclude(t => t.TourScheduleTickets)
                        .ThenInclude(t => t.Promotions)
                .Include(t => t.Reviews)
                .AsSplitQuery()
                .ToListAsync();

            tours = tours.OrderBy(t => pagedIds.IndexOf(t.Id)).ToList();

            return (tours, total);
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

            return query.Where(t => t.Name.Contains(term));
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
                .AsSplitQuery()
                .ToListAsync();

            return (tours, total);
        }

        public async Task<(List<Tour> Items, int TotalCount)> GetSaleTours(int page, int pageSize)
        {
            var now = DateTime.UtcNow;
            
            var query = _context.Tours
                .AsNoTracking()
                .Where(t => t.Status == "Active")
                .Where(t => t.TourSchedules.Any(s => s.DepartureDate > now && s.TourScheduleTickets.Any(ticket => 
                    ticket.Promotions.Any(p => p.Status == "Active" && p.StartDate <= now && p.EndDate >= now))));

            var totalCount = await query.CountAsync();

            var normalizedPageSize = Math.Clamp(pageSize, 1, 1000);
            var normalizedPage = Math.Max(1, page);

            var pagedIds = await query
                .OrderByDescending(t => t.Id)
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(t => t.Id)
                .ToListAsync();

            var tours = await _context.Tours
                .Where(t => pagedIds.Contains(t.Id))
                .Include(t => t.TourItineraries)
                .Include(t => t.TourSchedules)
                    .ThenInclude(t => t.TourScheduleTickets)
                        .ThenInclude(t => t.Promotions)
                .Include(t => t.Reviews)
                .AsSplitQuery()
                .ToListAsync();

            tours = tours.OrderBy(t => pagedIds.IndexOf(t.Id)).ToList();

            return (tours, totalCount);
        }

        public async Task<(List<Tour> Items, int TotalCount)> GetHotTours(int page, int pageSize)
        {
            var query = _context.Tours
                .AsNoTracking()
                .Where(t => t.Status == "Active")
                .OrderByDescending(t => t.TourSchedules
                    .SelectMany(s => s.TourScheduleTickets)
                    .Sum(st => st.SoldQuantity ?? 0));

            var totalCount = await query.CountAsync();

            var normalizedPageSize = Math.Clamp(pageSize, 1, 1000);
            var normalizedPage = Math.Max(1, page);

            var pagedIds = await query
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(t => t.Id)
                .ToListAsync();

            var tours = await _context.Tours
                .Where(t => pagedIds.Contains(t.Id))
                .Include(t => t.TourItineraries)
                .Include(t => t.TourSchedules)
                    .ThenInclude(t => t.TourScheduleTickets)
                        .ThenInclude(t => t.Promotions)
                .Include(t => t.Reviews)
                .AsSplitQuery()
                .ToListAsync();

            tours = tours.OrderBy(t => pagedIds.IndexOf(t.Id)).ToList();

            return (tours, totalCount);
        }

        public async Task<(List<Tour> Items, int TotalCount)> GetUpcomingTours(int page, int pageSize)
        {
            var now = DateTime.UtcNow;

            var query = _context.Tours
                .AsNoTracking()
                .Where(t => t.Status == "Active")
                .Where(t => t.TourSchedules.Any(s => s.DepartureDate > now));

            var totalCount = await query.CountAsync();
            var normalizedPageSize = Math.Clamp(pageSize, 1, 1000);
            var normalizedPage = Math.Max(1, page);

            var pagedIds = await query
                .OrderBy(t => t.TourSchedules
                    .Where(s => s.DepartureDate > now)
                    .Min(s => s.DepartureDate))
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(t => t.Id)
                .ToListAsync();

            var tours = await _context.Tours
                .Where(t => pagedIds.Contains(t.Id))
                .Include(t => t.TourItineraries)
                .Include(t => t.TourSchedules.Where(s => s.DepartureDate > now))
                    .ThenInclude(t => t.TourScheduleTickets)
                        .ThenInclude(t => t.Promotions)
                .Include(t => t.Reviews)
                .AsSplitQuery()
                .ToListAsync();

            tours = tours.OrderBy(t => pagedIds.IndexOf(t.Id)).ToList();

            return (tours, totalCount);
        }

        public async Task<(List<Tour> Items, int TotalCount)> GetToursByRegion(string region, int page, int pageSize)
        {
            var query = _context.Tours
                .AsNoTracking()
                .Where(t => t.Status == "Active");

            var r = region.Trim().ToLowerInvariant();
            string[] targetCities = r switch
            {
                "north" => new[] { "ha noi", "hai phong", "quang ninh", "lao cai", "ha giang", "yen bai", "lai chau", "dien bien", "son la", "hoa binh", "phu tho", "tuyen quang", "cao bang", "bac kan", "thai nguyen", "lang son", "bac giang", "bac ninh", "vinh phuc", "hai duong", "hung yen", "thai binh", "ha nam", "nam dinh", "ninh binh" },
                "central" => new[] { "thanh hoa", "nghe an", "ha tinh", "quang binh", "quang tri", "thua thien hue", "da nang", "quang nam", "quang ngai", "binh dinh", "phu yen", "khanh hoa", "ninh thuan", "binh thuan", "kon tum", "gia lai", "dak lak", "dak nong", "lam dong" },
                "south" => new[] { "ho chi minh", "ba ria", "vung tau", "binh duong", "binh phuoc", "dong nai", "tay ninh", "long an", "tien giang", "ben tre", "tra vinh", "vinh long", "dong thap", "an giang", "kien giang", "can tho", "hau giang", "soc trang", "bac lieu", "ca mau" },
                _ => Array.Empty<string>()
            };

            if (targetCities.Length > 0)
            {
                query = query.Where(t => t.City != null && targetCities.Contains(t.City.ToLower()));
            }
            else
            {
                return (new List<Tour>(), 0);
            }

            var now = DateTime.UtcNow;
            query = query.Where(t => t.TourSchedules.Any(s => s.DepartureDate > now));

            var totalCount = await query.CountAsync();
            var normalizedPageSize = Math.Clamp(pageSize, 1, 1000);
            var normalizedPage = Math.Max(1, page);

            var pagedIds = await query
                .OrderByDescending(t => t.Id)
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(t => t.Id)
                .ToListAsync();

            var tours = await _context.Tours
                .Where(t => pagedIds.Contains(t.Id))
                .Include(t => t.TourItineraries)
                .Include(t => t.TourSchedules.Where(s => s.DepartureDate > now))
                    .ThenInclude(t => t.TourScheduleTickets)
                        .ThenInclude(t => t.Promotions)
                .Include(t => t.Reviews)
                .AsSplitQuery()
                .ToListAsync();

            tours = tours.OrderBy(t => pagedIds.IndexOf(t.Id)).ToList();

            return (tours, totalCount);
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

        public async Task<bool> IsNameDuplicateAsync(string name, int? excludeId = null)
        {
            var query = _context.Tours.Where(t => t.Name.ToLower() == name.ToLower());
            if (excludeId.HasValue)
            {
                query = query.Where(t => t.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }
    }
}
