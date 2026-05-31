using ContentAPI.Constants;
using ContentAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ContentAPI.Repositories.Implements
{
    public class TourismInformationRepository : ITourismInformationRepository
    {
        private readonly StayHubContentDbContext _context;

        public TourismInformationRepository(StayHubContentDbContext context)
        {
            _context = context;
        }

        public async Task<(List<TourismInformation> Items, int Total)> GetAllPagedAsync(
            int page,
            int pageSize,
            string? searchTerm,
            string? type,
            string? status,
            string? city)
        {
            var query = _context.TourismInformations.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim();
                query = query.Where(t =>
                    t.Name.Contains(keyword) ||
                    (t.Description != null && t.Description.Contains(keyword)) ||
                    (t.Address != null && t.Address.Contains(keyword)) ||
                    (t.City != null && t.City.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(t => t.Type == type.Trim());
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status.Trim());
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(t => t.City != null && t.City.Contains(city.Trim()));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<(List<TourismInformation> Items, int Total)> GetActivePagedAsync(int page, int pageSize)
        {
            var query = _context.TourismInformations
                .AsNoTracking()
                .Where(t => t.Status == TourismInformationConstants.StatusActive);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<TourismInformation?> GetByIdAsync(int id)
        {
            return await _context.TourismInformations.FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task AddAsync(TourismInformation model)
        {
            _context.TourismInformations.Add(model);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TourismInformation model)
        {
            _context.TourismInformations.Update(model);
            await _context.SaveChangesAsync();
        }
    }
}
