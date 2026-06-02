using SocialAPI.DTOs;

namespace SocialAPI.Services;

public interface IPlatformAnalyticsService
{
    Task<PlatformSocialStatsDTO> GetSocialStatsAsync();
}
