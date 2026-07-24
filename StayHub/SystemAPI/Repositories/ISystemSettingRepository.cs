using System.Collections.Generic;
using System.Threading.Tasks;
using SystemAPI.Models;

namespace SystemAPI.Repositories
{
    public interface ISystemSettingRepository
    {
        Task<IEnumerable<SystemSetting>> GetAllSettingsAsync();
        Task<SystemSetting?> GetSettingByKeyAsync(string key);
        Task UpdateSettingAsync(SystemSetting setting);
        Task AddSettingAsync(SystemSetting setting);
    }
}
