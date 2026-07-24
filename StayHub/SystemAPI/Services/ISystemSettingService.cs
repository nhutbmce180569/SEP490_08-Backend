using System.Collections.Generic;
using System.Threading.Tasks;
using SystemAPI.DTOs;
using SystemAPI.Models;

namespace SystemAPI.Services
{
    public interface ISystemSettingService
    {
        Task<IEnumerable<SystemSetting>> GetAllSettingsAsync();
        Task<SystemSettingDTO?> GetSettingByKeyAsync(string key);
        Task UpdateSettingsAsync(UpdateSystemSettingsDTO dto);
    }
}
