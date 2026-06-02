using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface IPlatformAnalyticsClients
    {
        Task<PlatformUserStatsData?> GetUserStatsAsync(DateTime? from, DateTime? to, string granularity);

        Task<PlatformOperationsStatsData?> GetOperationsStatsAsync(DateTime? from, DateTime? to);

        Task<PlatformVoucherStatsData?> GetVoucherStatsAsync();

        Task<PlatformSocialStatsData?> GetSocialStatsAsync();
    }
}
