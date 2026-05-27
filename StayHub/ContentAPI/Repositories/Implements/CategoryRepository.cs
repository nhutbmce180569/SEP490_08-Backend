using ContentAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ContentAPI.Repositories.Implements
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly StayHubContentDbContext _context;

        public CategoryRepository(StayHubContentDbContext context)
        {
            _context = context;
        }

        public async Task<(List<Category> Categories, int Total)> GetAllPaged(int page, int pageSize)
        {
            var query = _context.Categories.AsQueryable();

            int total = await query.CountAsync();

            var categories = await query
                .AsNoTracking()
                .OrderByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (categories, total);
        }
        public async Task<(List<Category> Categories, int Total)> SearchPagedAsync(string keyword, int page, int pageSize)
        {
            var query = _context.Categories.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(c => c.Name.Contains(keyword) || (c.Description != null && c.Description.Contains(keyword)));
            }

            int total = await query.CountAsync();

            var categories = await query
                .OrderByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (categories, total);
        }
        public async Task<(List<Category> Categories, int Total)> GetActiveCategoriesPaged(int page, int pageSize)
        {
            var query = _context.Categories.Where(c => c.IsActive == true).AsQueryable();

            int total = await query.CountAsync();

            var categories = await query
                .AsNoTracking()
                .OrderByDescending(c => c.Id) // Sắp xếp theo Id giảm dần
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (categories, total);
        }

        public async Task<Category?> GetById(int id)
        {
            return await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Category?> GetBySlug(string slug)
        {
            return await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == slug);
        }

        public async Task Add(Category model)
        {
            _context.Categories.Add(model);
            await _context.SaveChangesAsync();
        }

        public async Task Update(int id, Category model)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Entry(category).CurrentValues.SetValues(model);
                await _context.SaveChangesAsync();
            }
        }

        public async Task Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }
    }
}