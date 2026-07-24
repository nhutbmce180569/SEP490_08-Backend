using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SystemAPI.DTOs;
using SystemAPI.Services;

namespace SystemAPI.Controllers
{
    [Route("api/system-settings")]
    [ApiController]
    public class SystemSettingsController : ControllerBase
    {
        private readonly ISystemSettingService _systemSettingService;

        public SystemSettingsController(ISystemSettingService systemSettingService)
        {
            _systemSettingService = systemSettingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSettings()
        {
            var settings = await _systemSettingService.GetAllSettingsAsync();
            return Ok(settings);
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> GetSettingByKey(string key)
        {
            var setting = await _systemSettingService.GetSettingByKeyAsync(key);
            if (setting == null) return NotFound();
            return Ok(setting);
        }

        [HttpPut]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSettings([FromForm] UpdateSystemSettingsDTO dto)
        {
            await _systemSettingService.UpdateSettingsAsync(dto);
            return Ok(new { Message = "System settings updated successfully" });
        }
    }
}
