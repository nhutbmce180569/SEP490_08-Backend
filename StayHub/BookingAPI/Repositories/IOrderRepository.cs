using BookingAPI.DTOs;
using BookingAPI.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BookingAPI.Repositories
{
    public interface IOrderRepository
    {
        Task<bool> HasCompletedBookingAsync(int customerId, List<int> scheduleIds);
        Task<bool> HasBookingAsync(List<int> scheduleIds);
        Task<Order> AddAsync(Order order);
        Task<Order?> GetByIdAsync(int id);
        Task<Order?> GetByIdAndCustomerIdAsync(int id, int customerId);
        Task<IEnumerable<Order>> GetByScheduleIdAsync(int scheduleId);
        Task<IEnumerable<Order>> GetByUserIdAsync(int userId);
        Task<(List<Order> Orders, int Total)> GetByUserIdPagedAsync(int userId, int page, int pageSize);

        /// <summary>Orders with status Paid or Completed.</summary>
        Task<List<int>> GetCustomerIdsWithMinTotalSpendAsync(long minAmount, DateTime? periodFrom, DateTime? periodTo);

        Task<List<int>> GetTopCustomerIdsByTotalSpendAsync(int top, DateTime? periodFrom, DateTime? periodTo);

        Task<OrderAnalyticsOverviewDTO> GetOrderOverviewAsync(DateTime? from, DateTime? to);

        Task<CustomerOrderSegmentDTO> GetCustomerOrderSegmentsAsync(DateTime? from, DateTime? to, int totalCustomers);

        Task<List<OrderTrendPointDTO>> GetOrderTrendsAsync(DateTime from, DateTime to, string granularity);

        Task<List<TopCustomerOrderDTO>> GetTopCustomersAsync(int top, DateTime? from, DateTime? to);

        Task<List<CustomerOrderMetricsDTO>> GetCustomerOrderMetricsAsync(List<int> customerIds, DateTime? from, DateTime? to);

        Task<CustomerOrderMetricsDTO?> GetCustomerOrderMetricsByIdAsync(int customerId, DateTime? from, DateTime? to);

        Task<PlatformOperationsStatsDTO> GetPlatformOperationsStatsAsync(DateTime? from, DateTime? to);

        Task<bool> UpdateStatusAsync(int orderId, string status);
        Task<int> GetPendingTicketCountByScheduleAsync(int scheduleId, int? excludeOrderId = null);
        Task<bool> CancelOrderWithTicketsAsync(int orderId);
        Task<List<int>> GetEligibleScheduleIdsByUserIdAsync(int userId);
    }
}
