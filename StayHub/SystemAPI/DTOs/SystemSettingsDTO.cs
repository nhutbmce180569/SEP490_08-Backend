using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace SystemAPI.DTOs
{
    public class SystemSettingDTO
    {
        public string SettingKey { get; set; } = null!;
        public string SettingValue { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class UpdateSystemSettingsDTO
    {
        public List<SystemSettingUpdateItem> Settings { get; set; } = new List<SystemSettingUpdateItem>();
        
        public IFormFile? WebLogoFile { get; set; }
        public IFormFile? WebVideoLogoFile { get; set; }
    }

    public class SystemSettingUpdateItem
    {
        public string SettingKey { get; set; } = null!;
        public string? SettingValue { get; set; }
    }
}
