using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SystemAPI.DTOs;
using SystemAPI.Models;
using SystemAPI.Repositories;

namespace SystemAPI.Services.Implements
{
    public class SystemSettingService : ISystemSettingService
    {
        private readonly ISystemSettingRepository _systemSettingRepository;
        private readonly ICloudinaryService _cloudinaryService;

        public SystemSettingService(ISystemSettingRepository systemSettingRepository, ICloudinaryService cloudinaryService)
        {
            _systemSettingRepository = systemSettingRepository;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<IEnumerable<SystemSetting>> GetAllSettingsAsync()
        {
            return await _systemSettingRepository.GetAllSettingsAsync();
        }

        public async Task<SystemSettingDTO?> GetSettingByKeyAsync(string key)
        {
            var setting = await _systemSettingRepository.GetSettingByKeyAsync(key);
            if (setting == null) return null;

            return new SystemSettingDTO
            {
                SettingKey = setting.SettingKey,
                SettingValue = setting.SettingValue,
                Description = setting.Description
            };
        }

        public async Task UpdateSettingsAsync(UpdateSystemSettingsDTO dto)
        {
            // Process basic settings
            if (dto.Settings != null)
            {
                foreach (var item in dto.Settings)
                {
                    var setting = await _systemSettingRepository.GetSettingByKeyAsync(item.SettingKey);
                    if (setting != null)
                    {
                        if (string.IsNullOrWhiteSpace(item.SettingValue))
                        {
                            if (item.SettingKey == "WebVideoLogo" && !string.IsNullOrWhiteSpace(setting.SettingValue))
                            {
                                string? oldPublicId = _cloudinaryService.ExtractPublicIdFromUrl(setting.SettingValue);
                                if (!string.IsNullOrWhiteSpace(oldPublicId))
                                {
                                    await _cloudinaryService.DeleteVideoAsync(oldPublicId);
                                }
                            }
                            else if (item.SettingKey == "WebLogo" && !string.IsNullOrWhiteSpace(setting.SettingValue))
                            {
                                string? oldPublicId = _cloudinaryService.ExtractPublicIdFromUrl(setting.SettingValue);
                                if (!string.IsNullOrWhiteSpace(oldPublicId))
                                {
                                    await _cloudinaryService.DeleteImageAsync(oldPublicId);
                                }
                            }
                        }

                        setting.SettingValue = string.IsNullOrWhiteSpace(item.SettingValue) ? "" : item.SettingValue;
                        setting.UpdatedAt = DateTime.UtcNow;
                        await _systemSettingRepository.UpdateSettingAsync(setting);
                    }
                    else
                    {
                        // Create if not exists
                        await _systemSettingRepository.AddSettingAsync(new SystemSetting
                        {
                            SettingKey = item.SettingKey,
                            SettingValue = item.SettingValue ?? "",
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            // Process WebLogo Upload
            if (dto.WebLogoFile != null && dto.WebLogoFile.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(dto.WebLogoFile, "StayHub_System");
                if (uploadResult.Error == null)
                {
                    var webLogoSetting = await _systemSettingRepository.GetSettingByKeyAsync("WebLogo");
                    if (webLogoSetting != null)
                    {
                        // Delete old image if exists
                        string? oldPublicId = _cloudinaryService.ExtractPublicIdFromUrl(webLogoSetting.SettingValue);
                        if (!string.IsNullOrEmpty(oldPublicId))
                        {
                            await _cloudinaryService.DeleteImageAsync(oldPublicId);
                        }

                        webLogoSetting.SettingValue = uploadResult.SecureUrl.ToString();
                        webLogoSetting.UpdatedAt = DateTime.UtcNow;
                        await _systemSettingRepository.UpdateSettingAsync(webLogoSetting);
                    }
                    else
                    {
                        await _systemSettingRepository.AddSettingAsync(new SystemSetting
                        {
                            SettingKey = "WebLogo",
                            SettingValue = uploadResult.SecureUrl.ToString(),
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            // Process WebVideoLogo Upload
            if (dto.WebVideoLogoFile != null && dto.WebVideoLogoFile.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadVideoAsync(dto.WebVideoLogoFile, "StayHub_System");
                if (uploadResult.Error == null)
                {
                    var appLogoSetting = await _systemSettingRepository.GetSettingByKeyAsync("WebVideoLogo");
                    if (appLogoSetting != null)
                    {
                        // Delete old video if exists
                        string? oldPublicId = _cloudinaryService.ExtractPublicIdFromUrl(appLogoSetting.SettingValue);
                        if (!string.IsNullOrEmpty(oldPublicId))
                        {
                            await _cloudinaryService.DeleteVideoAsync(oldPublicId);
                        }

                        appLogoSetting.SettingValue = uploadResult.SecureUrl.ToString();
                        appLogoSetting.UpdatedAt = DateTime.UtcNow;
                        await _systemSettingRepository.UpdateSettingAsync(appLogoSetting);
                    }
                    else
                    {
                        await _systemSettingRepository.AddSettingAsync(new SystemSetting
                        {
                            SettingKey = "WebVideoLogo",
                            SettingValue = uploadResult.SecureUrl.ToString(),
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
        }
    }
}
