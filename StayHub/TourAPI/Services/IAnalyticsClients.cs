using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface IAuthAnalyticsClient
    {
        Task<AuthDemographicsData?> GetDemographicsAsync(DateTime? from, DateTime? to, string granularity);

        Task<AuthCustomerListData?> GetCustomerListAsync(string? search, int page, int pageSize);

        Task<AuthCustomerSummary?> GetCustomerSummaryAsync(int customerId);

        Task<List<AuthCustomerSummary>> GetCustomersBatchAsync(List<int> customerIds);
    }

    public interface IBookingAnalyticsClient
    {
        Task<BookingOverviewData?> GetOverviewAsync(DateTime? from, DateTime? to);

        Task<BookingSegmentsData?> GetSegmentsAsync(DateTime? from, DateTime? to, int totalCustomers);

        Task<List<BookingTrendPoint>> GetTrendsAsync(DateTime? from, DateTime? to, string granularity);

        Task<List<BookingTopCustomer>> GetTopCustomersAsync(int top, DateTime? from, DateTime? to);

        Task<List<BookingCustomerMetrics>> GetCustomerMetricsAsync(List<int> customerIds, DateTime? from, DateTime? to);

        Task<BookingCustomerMetrics?> GetCustomerMetricsByIdAsync(int customerId, DateTime? from, DateTime? to);
    }
}
