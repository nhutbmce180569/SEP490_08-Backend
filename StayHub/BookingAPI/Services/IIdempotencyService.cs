using BookingAPI.Models;

namespace BookingAPI.Services
{
    public interface IIdempotencyService
    {
        Task<bool> TryAcquireLockAsync(string idempotencyKey, int userId);
        Task<IdempotencyState?> GetStateAsync(string idempotencyKey, int userId);
        Task SetCompletedAsync(string idempotencyKey, int userId, int orderId);
        Task DeleteProcessingKeyAsync(string idempotencyKey, int userId);
    }
}
