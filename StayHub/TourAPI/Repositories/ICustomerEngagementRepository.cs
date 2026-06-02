using TourAPI.DTOs;

namespace TourAPI.Repositories
{
    public interface ICustomerEngagementRepository
    {
        Task<CustomerEngagementAnalyticsDTO> GetEngagementOverviewAsync(int totalCustomers);

        Task<Dictionary<int, int>> GetReviewCountsByCustomerAsync();

        Task<Dictionary<int, int>> GetWishlistCountsByCustomerAsync();

        Task<Dictionary<int, decimal>> GetAverageRatingsByCustomerAsync();
    }
}
