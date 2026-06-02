using BookingAPI.DTOs;

namespace BookingAPI.Services;

public interface IAuthApiClient
{
    Task<List<BatchUserProfileDTO>> GetUsersBatchAsync(List<int> userIds);
}
