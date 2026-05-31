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

        /// <summary>Đơn có Status Paid hoặc Completed.</summary>
        Task<List<int>> GetCustomerIdsWithMinTotalSpendAsync(long minAmount, DateTime? periodFrom, DateTime? periodTo);

        Task<List<int>> GetTopCustomerIdsByTotalSpendAsync(int top, DateTime? periodFrom, DateTime? periodTo);
        Task<bool> UpdateStatusAsync(int orderId, string status);
        Task<int> GetPendingTicketCountByScheduleAsync(int scheduleId, int? excludeOrderId = null);
        Task<bool> CancelOrderWithTicketsAsync(int orderId);
        Task<List<int>> GetEligibleScheduleIdsByUserIdAsync(int userId);
    }
}
