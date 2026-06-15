using SocialAPI.DTOs;

namespace SocialAPI.Services
{
    public interface IAuthApiClient
    {
        Task<Dictionary<int, UserProfileShortDto>> GetUserProfilesAsync(IEnumerable<int> userIds);
        Task<UserProfileShortDto?> GetUserProfileAsync(int userId);
    }
}
