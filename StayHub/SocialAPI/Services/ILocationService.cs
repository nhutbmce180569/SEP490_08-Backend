using SocialAPI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocialAPI.Services
{
    public interface ILocationService
    {
        Task PingLocationAsync(int currentUserId, LocationPingDto dto);
        Task<IEnumerable<FriendLocationResponseDto>> GetLiveFriendsLocationsAsync(int currentUserId);
        Task<IEnumerable<FriendLocationResponseDto>> GetLiveScheduleLocationsAsync(int scheduleId);
        Task<string> GenerateTrackingTokenAsync(int currentUserId);
        Task<FriendLocationResponseDto> GetLocationByTrackingTokenAsync(string token);
    }
}
