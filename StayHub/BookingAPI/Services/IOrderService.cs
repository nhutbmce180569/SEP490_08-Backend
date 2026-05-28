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
        Task<PaginationDTO<ReadOrderDTO>> GetOrdersByUserIdAsync(int userId, int page, int pageSize);
        Task<bool> MarkOrderPaidAsync(int orderId, string customerEmail);
        Task<bool> CancelOrderAsync(int orderId);
    }
}
