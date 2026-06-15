using BookingAPI.Models;

namespace BookingAPI.Repositories
{
    public interface ICancellationRepository
    {
        Task<Order?> GetOrderForCancellationAsync(int orderId, int customerId);
        Task CreateCancellationRequestAsync(CancellationRequest request);
        Task<(IEnumerable<CancellationRequest> Data, int Total)> GetAllCancellationRequestsAsync(
            IReadOnlyCollection<int> scheduleIds,
            string? status,
            int page,
            int pageSize);
        Task<CancellationRequest?> GetCancellationRequestByIdAsync(int id);
        Task UpdateCancellationRequestAsync(CancellationRequest request);
    }
}
