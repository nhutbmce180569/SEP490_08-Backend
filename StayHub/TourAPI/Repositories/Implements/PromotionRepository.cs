using Microsoft.EntityFrameworkCore;
using TourAPI.Models;

namespace TourAPI.Repositories.Implements
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly StayHubCatalogDbContext _context;

        public PromotionRepository(StayHubCatalogDbContext context)
        {
            _context = context;
        }

        public async Task Add(Promotion model)
        {
            await _context.Promotions.AddAsync(model);
        }

        public void Delete(Promotion model)
        {
            _context.Promotions.Remove(model);
        }

        public async Task<(List<Promotion> Promotions, int Total)> GetAll(int page, int pageSize, string? searchTerm = null, string? status = null)
        {
            var query = _context.Promotions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm) || p.Code.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var total = await query.CountAsync();

            var promotions = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (promotions, total);
        }

        public async Task<Promotion?> GetByCode(string code)
        {
            return await _context.Promotions.FirstOrDefaultAsync(p => p.Code == code);
        }

        public async Task<Promotion?> GetById(int id)
        {
            return await _context.Promotions.FindAsync(id);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Update(Promotion model)
        {
            _context.Promotions.Update(model);
        }
    }
}
