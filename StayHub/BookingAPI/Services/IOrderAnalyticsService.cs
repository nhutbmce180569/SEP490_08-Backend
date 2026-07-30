using BookingAPI.DTOs;

namespace BookingAPI.Services
{
    public interface IOrderAnalyticsService
    {
        Task<OrderAnalyticsOverviewDTO> GetOverviewAsync(DateTime? from, DateTime? to);

        Task<CustomerOrderSegmentDTO> GetSegmentsAsync(DateTime? from, DateTime? to, int totalCustomers);

        Task<List<OrderTrendPointDTO>> GetTrendsAsync(DateTime? from, DateTime? to, string granularity);

        Task<BookingStatisticsResponseDTO> GetBookingStatisticsAsync(BookingStatisticsRequestDTO request);

        Task<List<TopCustomerOrderDTO>> GetTopCustomersAsync(int top, DateTime? from, DateTime? to);

        Task<List<CustomerOrderMetricsDTO>> GetCustomerMetricsAsync(List<int> customerIds, DateTime? from, DateTime? to);

        Task<CustomerOrderMetricsDTO?> GetCustomerMetricsByIdAsync(int customerId, DateTime? from, DateTime? to);

        (DateTime From, DateTime To) ResolveDateRange(DateTime? from, DateTime? to);
    }
}
