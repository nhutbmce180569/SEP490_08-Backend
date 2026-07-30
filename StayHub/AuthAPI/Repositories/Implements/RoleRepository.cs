using AuthAPI.Models;
using AuthAPI.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Repositories.Implements
{
    public class RoleRepository : IRoleRepository
    {
        private readonly StayHubIdentityDbContext _context;

        public RoleRepository(StayHubIdentityDbContext context)
        {
            _context = context;
        }

        public async Task<Role?> GetByName(string name)
        {
            try
            {
                return await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == name);
            }
            catch (Exception e)
            {
                throw new Exception($"Error retrieving role by name: {e.Message}");
            }
        }
        public async Task<Role?> GetById(int id)
        {
            try
            {
                return await _context.Roles
                    .FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception e)
            {
                throw new Exception($"Error retrieving role by id: {e.Message}");
            }
        }
        public async Task<List<Role>> GetAll()
        {
            try
            {
                return await _context.Roles.ToListAsync();
            }
            catch (Exception e)
            {
                throw new Exception($"Error retrieving all roles: {e.Message}");
            }
        }
    }
}