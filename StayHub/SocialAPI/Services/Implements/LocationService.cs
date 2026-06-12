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
        private readonly IHubContext<TrackingHub> _trackingHubContext;

        public LocationService(
            StayHubSocialDbContext context,
            IConnectionMultiplexer redis,
            IHttpClientFactory httpClientFactory,
            IHubContext<FriendshipHub> hubContext,
            IHubContext<TrackingHub> trackingHubContext)
        {
            _context = context;
            _redis = redis;
            _httpClientFactory = httpClientFactory;
            _hubContext = hubContext;
            _trackingHubContext = trackingHubContext;
        }

        public async Task PingLocationAsync(int currentUserId, LocationPingDto dto)
        {
            // 1. Lưu tọa độ vào DB
            try
            {
                var locationLog = new LocationLog
                {
                    UserId = currentUserId,
                    Lat = dto.Lat,
                    Lng = dto.Lng,
                    ScheduleId = dto.ScheduleId ?? 0,
                    Timestamp = DateTime.UtcNow
                };
                await _context.LocationLogs.AddAsync(locationLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Bỏ qua lỗi DB để không làm gián đoạn việc cập nhật Redis và SignalR (Live Tracking)
            }

            // 2. Lưu tọa độ mới nhất vào Redis (TTL 30 phút)
            var db = _redis.GetDatabase();
            var redisKey = $"live_loc_{currentUserId}";
            var locData = new { lat = dto.Lat, lng = dto.Lng, lastUpdated = DateTime.UtcNow };
            var jsonLoc = JsonSerializer.Serialize(locData);
            await db.StringSetAsync(redisKey, jsonLoc, TimeSpan.FromMinutes(30));

            if (dto.ScheduleId.HasValue && dto.ScheduleId.Value > 0)
            {
                var scheduleLiveKey = $"schedule_live_{dto.ScheduleId.Value}";
                await db.SetAddAsync(scheduleLiveKey, currentUserId);
                await db.KeyExpireAsync(scheduleLiveKey, TimeSpan.FromMinutes(30));
            }

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

            // 6. Bắn SignalR cho public tracking
            var userTokensKey = $"user_tokens_{currentUserId}";
            var activeTokens = await db.SetMembersAsync(userTokensKey);
            if (activeTokens != null && activeTokens.Length > 0)
            {
                foreach (var tokenVal in activeTokens)
                {
                    await _trackingHubContext.Clients.Group(tokenVal.ToString()).SendAsync("ReceivePublicLocation", responseDto);
                }
            }

            if (dto.ScheduleId.HasValue && dto.ScheduleId.Value > 0)
            {
                var tourGroupName = $"tour_{dto.ScheduleId.Value}";
                await _trackingHubContext.Clients.Group(tourGroupName).SendAsync("ReceiveTourLocationUpdate", responseDto);
            }
        }

        public async Task<IEnumerable<FriendLocationResponseDto>> GetLiveScheduleLocationsAsync(int scheduleId)
        {
            if (scheduleId <= 0)
            {
                return new List<FriendLocationResponseDto>();
            }

            var db = _redis.GetDatabase();
            var scheduleLiveKey = $"schedule_live_{scheduleId}";
            var members = await db.SetMembersAsync(scheduleLiveKey);

            if (members == null || members.Length == 0)
            {
                return new List<FriendLocationResponseDto>();
            }

            var liveLocations = new List<FriendLocationResponseDto>();
            var onlineUserIds = new List<int>();

            foreach (var member in members)
            {
                if (int.TryParse(member.ToString(), out var userId))
                {
                    onlineUserIds.Add(userId);
                }
            }

            foreach (var userId in onlineUserIds)
            {
                var redisKey = $"live_loc_{userId}";
                var redisValue = await db.StringGetAsync(redisKey);
                if (!redisValue.HasValue)
                {
                    continue;
                }

                try
                {
                    using var doc = JsonDocument.Parse((string)redisValue);
                    var root = doc.RootElement;
                    var lat = root.GetProperty("lat").GetDouble();
                    var lng = root.GetProperty("lng").GetDouble();
                    var lastUpdated = root.GetProperty("lastUpdated").GetDateTime();

                    liveLocations.Add(new FriendLocationResponseDto
                    {
                        UserId = userId,
                        Lat = lat,
                        Lng = lng,
                        LastUpdated = lastUpdated
                    });
                }
                catch
                {
                    // Ignore malformed Redis entries
                }
            }

            if (!liveLocations.Any())
            {
                return new List<FriendLocationResponseDto>();
            }

            var userProfiles = new Dictionary<int, UserProfileShortDto>();
            try
            {
                using var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync("https://localhost:7001/api/users/batch", onlineUserIds);
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
                // Ignore if AuthAPI is unavailable
            }

            foreach (var location in liveLocations)
            {
                if (userProfiles.TryGetValue(location.UserId, out var profile))
                {
                    location.FullName = profile.FullName ?? "Anonymous";
                    location.AvatarUrl = profile.AvatarUrl;
                }
                else
                {
                    location.FullName = "Anonymous";
                }
            }

            return liveLocations;
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

        public async Task<string> GenerateTrackingTokenAsync(int currentUserId)
        {
            var token = Guid.NewGuid().ToString("N"); // Tạo GUID không gạch ngang
            var db = _redis.GetDatabase();

            // Lưu cặp Token - UserId (Tồn tại 24 giờ)
            var trackingKey = $"tracking_{token}";
            await db.StringSetAsync(trackingKey, currentUserId, TimeSpan.FromHours(24));

            // Lưu Token này vào danh sách token đang hoạt động của người dùng (Để sau này lấy tên Group SignalR)
            var userTokensKey = $"user_tokens_{currentUserId}";
            await db.SetAddAsync(userTokensKey, token);
            await db.KeyExpireAsync(userTokensKey, TimeSpan.FromHours(24));

            return token;
        }

        public async Task<FriendLocationResponseDto> GetLocationByTrackingTokenAsync(string token)
        {
            var db = _redis.GetDatabase();
            var trackingKey = $"tracking_{token}";
            var userIdVal = await db.StringGetAsync(trackingKey);

            if (!userIdVal.HasValue || !int.TryParse(userIdVal.ToString(), out int userId))
            {
                throw new KeyNotFoundException("Tracking token is invalid or has expired.");
            }

            var locKey = $"live_loc_{userId}";
            var locVal = await db.StringGetAsync(locKey);

            if (!locVal.HasValue)
            {
                throw new KeyNotFoundException("User's live location is currently unavailable.");
            }

            try
            {
                using var doc = JsonDocument.Parse(locVal.ToString());
                var root = doc.RootElement;
                var lat = root.GetProperty("lat").GetDouble();
                var lng = root.GetProperty("lng").GetDouble();
                var lastUpdated = root.GetProperty("lastUpdated").GetDateTime();

                return new FriendLocationResponseDto
                {
                    UserId = userId,
                    FullName = "Anonymous user", // Tối ưu: Không gọi AuthAPI cho tính năng public để giảm tải
                    AvatarUrl = null,
                    Lat = lat,
                    Lng = lng,
                    LastUpdated = lastUpdated
                };
            }
            catch (Exception)
            {
                throw new KeyNotFoundException("Location data is corrupt or cannot be parsed.");
            }
        }

        public async Task<IEnumerable<FootprintDto>> GetMyFootprintsAsync(int userId)
        {
            try
            {
                var footprints = await _context.LocationLogs
                    .AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .OrderBy(x => x.Timestamp)
                    .Select(x => new FootprintDto
                    {
                        Lat = x.Lat,
                        Lng = x.Lng,
                        Timestamp = x.Timestamp
                    })
                    .ToListAsync();

                return footprints;
            }
            catch (Exception)
            {
                return new List<FootprintDto>();
            }
        }
    }
}
