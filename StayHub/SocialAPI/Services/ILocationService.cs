using SocialAPI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocialAPI.Services
{
    public interface ILocationService
    {
        Task PingLocationAsync(int currentUserId, LocationPingDto dto);
        Task<IEnumerable<FriendLocationResponseDto>> GetLiveFriendsLocationsAsync(int currentUserId);
        Task<string> GenerateTrackingTokenAsync(int currentUserId);
        Task<FriendLocationResponseDto> GetLocationByTrackingTokenAsync(string token);
    }
}
