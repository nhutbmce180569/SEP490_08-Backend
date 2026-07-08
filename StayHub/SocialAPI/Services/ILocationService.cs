using SocialAPI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocialAPI.Services
{
    public interface ILocationService
    {
        Task PingLocationAsync(int currentUserId, LocationPingDto dto);
        Task<IEnumerable<FriendLocationResponseDto>> GetLiveFriendsLocationsAsync(int currentUserId);
        Task<IEnumerable<LiveScheduleMemberLocationDto>> GetLiveScheduleLocationsAsync(int scheduleId);
        Task<string> GenerateTrackingTokenAsync(int currentUserId);
        Task<FriendLocationResponseDto> GetLocationByTrackingTokenAsync(string token);
        Task<IEnumerable<HeatPointDto>> GetHeatmapDataAsync(int? scheduleId, string type, int days);
        Task<IEnumerable<FootprintDto>> GetMyFootprintsAsync(int userId);
    }
}
