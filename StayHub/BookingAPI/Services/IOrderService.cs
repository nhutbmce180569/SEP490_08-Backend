using BookingAPI.DTOs;
using System.Threading.Tasks;

namespace BookingAPI.Services
{
    public interface IOrderService
    {
        Task<bool> CheckCompletedBookingAsync(CheckBookingRequest request);
        Task<bool> CheckBookingAsync(CheckBookingTour request);
        Task<ReadOrderDTO> CreateOrderAsync(int customerId, CreateOrderDTO request);
        Task<ReadOrderDTO?> GetOrderByIdAsync(int id, int customerId);
        Task<IEnumerable<ReadOrderDTO>> GetOrdersByScheduleIdAsync(int scheduleId);
        Task<IEnumerable<ScheduleCustomerDTO>> GetScheduleCustomersAsync(int scheduleId, string? attendeeName = null);
        Task<PaginationDTO<ReadOrderDTO>> GetOrdersByUserIdAsync(int userId, int page, int pageSize, string? status = null);
        Task<bool> MarkOrderPaidAsync(int orderId, string customerEmail);
        Task<bool> CancelOrderAsync(int orderId);
        Task<List<int>> GetCustomerIdsByScheduleIdAsync(int scheduleId);
        Task<List<int>> GetEligibleScheduleIdsByUserIdAsync(int userId);
    }
}
