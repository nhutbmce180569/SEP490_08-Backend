using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SocialAPI.DTOs;
using SocialAPI.Hubs;
using SocialAPI.Models;
using StackExchange.Redis;

namespace SocialAPI.Services.Implements
{
    public class LocationService : ILocationService
    {
        private readonly StayHubSocialDbContext _context;
        private readonly IConnectionMultiplexer _redis;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHubContext<FriendshipHub> _hubContext;

        public LocationService(
            StayHubSocialDbContext context,
            IConnectionMultiplexer redis,
            IHttpClientFactory httpClientFactory,
            IHubContext<FriendshipHub> hubContext)
        {
            _context = context;
            _redis = redis;
            _httpClientFactory = httpClientFactory;
            _hubContext = hubContext;
        }

        public async Task PingLocationAsync(int currentUserId, LocationPingDto dto)
        {
            // 1. Lưu tọa độ vào DB
            var locationLog = new LocationLog
            {
                UserId = currentUserId,
                Lat = dto.Lat,
                Lng = dto.Lng,
                ScheduleId = dto.ScheduleId ?? 0,
                Timestamp = DateTime.UtcNow
            };
            _context.LocationLogs.Add(locationLog);
            await _context.SaveChangesAsync();

            // 2. Lưu tọa độ mới nhất vào Redis (TTL 30 phút)
            var db = _redis.GetDatabase();
            var redisKey = $"live_loc_{currentUserId}";
            var locData = new { lat = dto.Lat, lng = dto.Lng, lastUpdated = DateTime.UtcNow };
            var jsonLoc = JsonSerializer.Serialize(locData);
            await db.StringSetAsync(redisKey, jsonLoc, TimeSpan.FromMinutes(30));

            // 3. Lấy danh sách bạn bè
            var friendIds = await _context.Friendships
                .Where(f => f.Status == "Accepted" && (f.RequesterId == currentUserId || f.ReceiverId == currentUserId))
                .Select(f => f.RequesterId == currentUserId ? f.ReceiverId : f.RequesterId)
                .ToListAsync();

            if (!friendIds.Any()) return;

            // 4. Lấy Profile của currentUserId từ AuthAPI
            var userProfile = new UserProfileShortDto { Id = currentUserId, FullName = "Anonymous", AvatarUrl = null };
            try
            {
                using var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync("https://localhost:7001/api/users/batch", new List<int> { currentUserId });
                if (response.IsSuccessStatusCode)
                {
                    var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserProfileShortDto>>>();
                    var profile = apiResult?.Data?.FirstOrDefault();
                    if (profile != null)
                    {
                        userProfile = profile;
                    }
                }
            }
            catch
            {
                // Ignore if AuthAPI is down
            }

            // 5. Bắn SignalR cho bạn bè
            var responseDto = new FriendLocationResponseDto
            {
                UserId = currentUserId,
                FullName = userProfile.FullName ?? "Anonymous",
                AvatarUrl = userProfile.AvatarUrl,
                Lat = dto.Lat,
                Lng = dto.Lng,
                LastUpdated = locData.lastUpdated
            };

            foreach (var friendId in friendIds)
            {
                await _hubContext.Clients.User(friendId.ToString()).SendAsync("ReceiveFriendLocation", responseDto);
            }
        }

        public async Task<IEnumerable<FriendLocationResponseDto>> GetLiveFriendsLocationsAsync(int currentUserId)
        {
            // 1. Lấy danh sách bạn bè
            var friendIds = await _context.Friendships
                .Where(f => f.Status == "Accepted" && (f.RequesterId == currentUserId || f.ReceiverId == currentUserId))
                .Select(f => f.RequesterId == currentUserId ? f.ReceiverId : f.RequesterId)
                .ToListAsync();

            if (!friendIds.Any()) return new List<FriendLocationResponseDto>();

            // 2. Lấy dữ liệu từ Redis
            var db = _redis.GetDatabase();
            var liveFriends = new List<FriendLocationResponseDto>();
            var onlineFriendIds = new List<int>();

            foreach (var friendId in friendIds)
            {
                var redisKey = $"live_loc_{friendId}";
                var redisValue = await db.StringGetAsync(redisKey);

                if (redisValue.HasValue)
                {
                    try
                    {
                        // Bắt try-catch an toàn khi parse JSON
                        using var doc = JsonDocument.Parse(redisValue.ToString());
                        var root = doc.RootElement;
                        var lat = root.GetProperty("lat").GetDouble();
                        var lng = root.GetProperty("lng").GetDouble();
                        var lastUpdated = root.GetProperty("lastUpdated").GetDateTime();

                        liveFriends.Add(new FriendLocationResponseDto
                        {
                            UserId = friendId,
                            Lat = lat,
                            Lng = lng,
                            LastUpdated = lastUpdated
                        });
                        onlineFriendIds.Add(friendId);
                    }
                    catch
                    {
                        // Ignore parse errors (Dữ liệu rác trên Redis sẽ bị bỏ qua)
                    }
                }
            }

            if (!onlineFriendIds.Any()) return new List<FriendLocationResponseDto>();

            // 3. Lấy Profile từ AuthAPI
            var userProfiles = new Dictionary<int, UserProfileShortDto>();
            try
            {
                using var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync("https://localhost:7001/api/users/batch", onlineFriendIds);
                if (response.IsSuccessStatusCode)
                {
                    var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserProfileShortDto>>>();
                    if (apiResult?.Data != null)
                    {
                        userProfiles = apiResult.Data.ToDictionary(u => u.Id, u => u);
                    }
                }
            }
            catch
            {
                // Fallback nếu AuthAPI sập
            }

            // 4. Map Profile vào Dto
            foreach (var lf in liveFriends)
            {
                if (userProfiles.TryGetValue(lf.UserId, out var profile))
                {
                    lf.FullName = profile.FullName ?? "Anonymous";
                    lf.AvatarUrl = profile.AvatarUrl;
                }
                else
                {
                    lf.FullName = "Anonymous";
                }
            }

            return liveFriends;
        }
    }
}
