using BookingAPI.Models;

namespace BookingAPI.Repositories
{
    public interface ICancellationRepository
    {
        Task<Order?> GetOrderForCancellationAsync(int orderId, int customerId);
        Task CreateCancellationRequestAsync(CancellationRequest request);
        Task<IEnumerable<CancellationRequest>> GetAllCancellationRequestsAsync(string? status);
        Task<CancellationRequest?> GetCancellationRequestByIdAsync(int id);
        Task UpdateCancellationRequestAsync(CancellationRequest request);
    }
}
