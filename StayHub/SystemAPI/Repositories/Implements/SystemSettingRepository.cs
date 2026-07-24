using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using SystemAPI.Models;

namespace SystemAPI.Repositories.Implements
{
    public class SystemSettingRepository : ISystemSettingRepository
    {
        private readonly StayHubSystemDbContext _context;

        public SystemSettingRepository(StayHubSystemDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SystemSetting>> GetAllSettingsAsync()
        {
            return await _context.SystemSettings.ToListAsync();
        }

        public async Task<SystemSetting?> GetSettingByKeyAsync(string key)
        {
            return await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
        }

        public async Task UpdateSettingAsync(SystemSetting setting)
        {
            _context.SystemSettings.Update(setting);
            await _context.SaveChangesAsync();
        }

        public async Task AddSettingAsync(SystemSetting setting)
        {
            await _context.SystemSettings.AddAsync(setting);
            await _context.SaveChangesAsync();
        }
    }
}
