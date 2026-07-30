using ContentAPI.Models;
using ContentAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using System;

namespace ContentAPI.Repositories.Implements
{
    public class BannerRepository : IBannerRepository
    {
        private readonly StayHubContentDbContext _context;

        public BannerRepository(StayHubContentDbContext context)
        {
            _context = context;
        }

        public async Task<(List<Banner> Banners, int Total)> GetAllPaged(int page, int pageSize)
        {
            var query = _context.Banners.AsQueryable();

            int total = await query.CountAsync();

            var banners = await query
                .OrderByDescending(b => b.Priority) // Sắp xếp theo Priority giảm dần
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (banners, total);
        }

        public async Task<(List<Banner> Banners, int Total)> SearchPagedAsync(string keyword, int page, int pageSize)
        {
            var query = _context.Banners.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(b => b.Title.Contains(keyword));
            }

            int total = await query.CountAsync();

            var banners = await query
                .OrderByDescending(b => b.Priority)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (banners, total);
        }

        public async Task<(List<Banner> Banners, int Total)> GetActiveBannersPaged(int page, int pageSize)
        {
            var query = _context.Banners.Where(b => b.IsActive == true).AsQueryable();

            int total = await query.CountAsync();

            var banners = await query
                .OrderByDescending(b => b.Priority)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (banners, total);
        }

        public async Task<Banner?> GetById(int id)
        {
            return await _context.Banners.FindAsync(id);
        }

        public async Task<Banner> Add(Banner banner)
        {
            _context.Banners.Add(banner);
            await _context.SaveChangesAsync();
            return banner;
        }

        public async Task Update(int id, Banner banner)
        {
            _context.Banners.Update(banner);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var banner = await GetById(id);
            if (banner != null)
            {
                _context.Banners.Remove(banner);
                await _context.SaveChangesAsync();
            }
        }
    }
}