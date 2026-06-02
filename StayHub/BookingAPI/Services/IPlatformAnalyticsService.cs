using BookingAPI.DTOs;

namespace BookingAPI.Services
{
    public interface IPlatformAnalyticsService
    {
        Task<PlatformOperationsStatsDTO> GetOperationsStatsAsync(DateTime? from, DateTime? to);
    }
}
