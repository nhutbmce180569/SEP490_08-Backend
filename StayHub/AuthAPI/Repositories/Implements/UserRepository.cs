using AuthAPI.DTOs;
using AuthAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Repositories.Implements
{
    public class UserRepository : IUserRepository
    {
        private readonly StayHubIdentityDbContext _context;

        public UserRepository(StayHubIdentityDbContext context)
        {
            _context = context;
        }

        public async Task Add(User model)
        {
            _context.Users.Add(model);
            await _context.SaveChangesAsync();
        }

        public async Task<List<User>> GetAll()
        {
            return await _context.Users
                .Include(r => r.Roles)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(List<User> Users, int Total)> GetAllPaged(int page, int pageSize)
        {
            var query = _context.Users
                .Include(r => r.Roles)
                .Where(u => !u.Roles.Any(r => r.Name == "Admin"))
                .AsNoTracking();

            int total = await query.CountAsync();

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, total);
        }

        public async Task<User?> GetById(int id)
        {
            return await _context.Users
                .Include(r => r.Roles)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmail(string email)
        {
            return await _context.Users
                .AsNoTracking()
                .Include(r => r.Roles)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task Update(int id, User model)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Entry(user).CurrentValues.SetValues(model);
                await _context.SaveChangesAsync();
            }
        }

        public async Task Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(List<User> Users, int Total)> SearchPagedAsync(string query, int page, int pageSize, string? roleName = null)
        {
            var queryable = _context.Users
                .Include(u => u.Roles)
                .Where(u => u.FullName.Contains(query) || u.Email.Contains(query))
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                queryable = queryable.Where(u => u.Roles.Any(r => r.Name == roleName));
            }

            // Lấy tổng số lượng kết quả thỏa mãn điều kiện tìm kiếm
            int total = await queryable.CountAsync();

            // Lấy dữ liệu của trang hiện tại
            var users = await queryable
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, total);
        }


        public async Task<List<User>> GetUsersByIdsAsync(List<int> ids)
        {
            return await _context.Users
                .Include(u => u.Roles)
                .Where(u => ids.Contains(u.Id))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(List<User> Users, int Total)> FilterPagedAsync(UserFilterDTO filter)
        {
            var query = _context.Users
                .Include(u => u.Roles)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Email))
            {
                query = query.Where(u => u.Email.Contains(filter.Email));
            }

            if (!string.IsNullOrWhiteSpace(filter.FullName))
            {
                query = query.Where(u => u.FullName.Contains(filter.FullName));
            }

            if (!string.IsNullOrWhiteSpace(filter.PhoneNumber))
            {
                query = query.Where(u => u.PhoneNumber != null && u.PhoneNumber.Contains(filter.PhoneNumber));
            }

            if (filter.Roles != null && filter.Roles.Any())
            {
                query = query.Where(u => u.Roles.Any(r => filter.Roles.Contains(r.Name)));
            }

            int total = await query.CountAsync();

            var users = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (users, total);
        }

        public async Task<List<User>> GetAllCustomersAsync()
        {
            return await _context.Users
                .Include(u => u.Roles)
                .Where(u => u.Roles.Any(r => r.Name == "Customer"))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(List<User> Users, int Total)> GetCustomersPagedAsync(string? search, int page, int pageSize)
        {
            var query = _context.Users
                .Include(u => u.Roles)
                .Where(u => u.Roles.Any(r => r.Name == "Customer"))
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
            }

            var total = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, total);
        }
    }
}