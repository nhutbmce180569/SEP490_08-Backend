using VoucherAPI.DTOs;

namespace VoucherAPI.Services;

public interface IPlatformAnalyticsService
{
    Task<PlatformVoucherStatsDTO> GetVoucherStatsAsync();
}
