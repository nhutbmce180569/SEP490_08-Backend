using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface IPlatformAnalyticsService
    {
        Task<PlatformAnalyticsOverviewDTO> GetOverviewAsync(DateTime? from, DateTime? to);

        Task<PlatformUserAnalyticsDTO> GetUsersAsync(DateTime? from, DateTime? to);

        Task<PlatformCatalogAnalyticsDTO> GetCatalogAsync(int top);

        Task<PlatformVoucherAnalyticsDTO> GetVouchersAsync();

        Task<PlatformSocialAnalyticsDTO> GetSocialAsync();

        Task<PlatformHealthAnalyticsDTO> GetHealthAsync(DateTime? from, DateTime? to);
    }
}
