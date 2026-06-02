using TourAPI.DTOs;

namespace TourAPI.Repositories
{
    public interface IPlatformCatalogRepository
    {
        Task<PlatformCatalogAnalyticsDTO> GetCatalogStatsAsync(int topBookedTours);

        Task<decimal> GetReviewResponseRateAsync();
    }
}
