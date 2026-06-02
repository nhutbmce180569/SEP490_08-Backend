using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface ICustomerAnalyticsService
    {
        Task<CustomerAnalyticsOverviewDTO> GetOverviewAsync(DateTime? from, DateTime? to);

        Task<CustomerDemographicsAnalyticsDTO> GetDemographicsAsync(DateTime? from, DateTime? to);

        Task<CustomerSegmentAnalyticsDTO> GetSegmentsAsync(DateTime? from, DateTime? to);

        Task<CustomerTrendAnalyticsDTO> GetTrendsAsync(DateTime? from, DateTime? to, string granularity);

        Task<List<TopCustomerAnalyticsDTO>> GetTopCustomersAsync(int top, DateTime? from, DateTime? to);

        Task<CustomerEngagementAnalyticsDTO> GetEngagementAsync();

        Task<PaginationDTO<CustomerListItemAnalyticsDTO>> GetCustomerListAsync(CustomerListQueryDTO query);

        Task<CustomerDetailAnalyticsDTO?> GetCustomerDetailAsync(int customerId, DateTime? from, DateTime? to);
    }
}
