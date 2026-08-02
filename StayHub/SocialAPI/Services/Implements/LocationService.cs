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
        private readonly IBookingApiClient _bookingApiClient;
        private readonly IHubContext<TrackingHub> _trackingHubContext;
        private readonly IAuthApiClient _authApiClient;
        private readonly ITourApiClient _tourApiClient;

        public LocationService(
            StayHubSocialDbContext context,
            IConnectionMultiplexer redis,
            IHttpClientFactory httpClientFactory,
            IHubContext<FriendshipHub> hubContext,
            IHubContext<TrackingHub> trackingHubContext,
            IAuthApiClient authApiClient,
            IBookingApiClient bookingApiClient,
            ITourApiClient tourApiClient)
        {
            _context = context;
            _redis = redis;
            _httpClientFactory = httpClientFactory;
            _hubContext = hubContext;
            _trackingHubContext = trackingHubContext;
            _authApiClient = authApiClient;
            _bookingApiClient = bookingApiClient;
            _tourApiClient = tourApiClient;
        }

        public async Task PingLocationAsync(int currentUserId, LocationPingDto dto, string userRole = "Customer")
        {
            // Rate limit: 1 ping / giây / user (chống các client gọi liên tục)
            var db = _redis.GetDatabase();
            var rateLimitKey = $"ping_rl_{currentUserId}";
            if (!await db.StringSetAsync(rateLimitKey, 1, TimeSpan.FromSeconds(1), When.NotExists))
            {
                return; // Bỏ qua ping nếu đã ping trong 1 giây vừa rồi
            }

            // Exclusive tracking logic (Mobile Priority)
            var activePlatformKey = $"active_platform_{currentUserId}";
            var activePlatform = await db.StringGetAsync(activePlatformKey);

            if (dto.Platform == "Web" && activePlatform.HasValue && activePlatform.ToString() == "Mobile")
            {
                throw new InvalidOperationException("Tracking is exclusively active on Mobile device.");
            }

            if (!string.IsNullOrEmpty(dto.Platform))
            {
                // TTL 60 seconds. Mobile pings every 12 seconds, so it will keep the lock alive.
                await db.StringSetAsync(activePlatformKey, dto.Platform, TimeSpan.FromSeconds(60));

                if (dto.Platform == "Mobile")
                {
                    // Thông báo cho Web biết rằng Mobile đang giữ quyền (kick Web ra khỏi trạng thái Tracking)
                    await _hubContext.Clients.User(currentUserId.ToString()).SendAsync("ForceStopTracking", "Mobile");
                }
            }

            // 1. Lưu tọa độ vào DB (giới hạn tần suất ghi DB: tối đa 1 lần mỗi 30 giây cho mỗi user)
            try
            {
                var dbLogLimitKey = $"db_log_rl_{currentUserId}";
                if (await db.StringSetAsync(dbLogLimitKey, 1, TimeSpan.FromSeconds(30), When.NotExists))
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
            }
            catch (Exception)
            {
                // Bỏ qua lỗi DB để không làm gián đoạn việc cập nhật Redis và SignalR (Live Tracking)
            }

            // 2. Lưu tọa độ mới nhất vào Redis (TTL 30 phút)
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

            // 4. Lấy Profile của currentUserId từ AuthAPI (dùng abstraction thay vì hardcode URL)
            var userProfile = new UserProfileShortDto { Id = currentUserId, FullName = "Anonymous", AvatarUrl = null };
            try
            {
                var profiles = await _authApiClient.GetUserProfilesAsync(new List<int> { currentUserId });
                if (profiles.TryGetValue(currentUserId, out var profile))
                {
                    userProfile = profile;
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
                LastUpdated = locData.lastUpdated,
                Role = userRole
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

        public async Task<IEnumerable<LiveScheduleMemberLocationDto>> GetLiveScheduleLocationsAsync(int scheduleId, int userId, string userRole, string? bearerToken)
        {
            if (scheduleId <= 0) return [];

            // 1. Lấy thông tin schedule từ TourAPI (staff & manager)
            var metadata = await _tourApiClient.GetScheduleMetadataAsync(scheduleId, bearerToken);
            if (metadata == null) return [];

            // 2. Lấy danh sách customer đã đặt mua từ BookingAPI
            var bookedCustomerIds = await _bookingApiClient.GetCustomerIdsByScheduleAsync(scheduleId);

            // 3. Phân quyền và xác định đối tượng được phép hiển thị
            bool isAuthorized = false;
            var allowedUserIds = new HashSet<int>();

            if (userRole == "Admin")
            {
                throw new UnauthorizedAccessException("Admin does not have map viewing capability.");
            }
            else if (metadata.TourCreatedBy == userId)
            {
                isAuthorized = true;
                allowedUserIds.UnionWith(metadata.StaffIds); // Manager only sees Staff
            }
            else if (metadata.StaffIds.Contains(userId))
            {
                isAuthorized = true;
                allowedUserIds.UnionWith(bookedCustomerIds); // Staff sees Customers
                allowedUserIds.UnionWith(metadata.StaffIds); // ...and other Staff members
            }
            else if (bookedCustomerIds.Contains(userId))
            {
                isAuthorized = true;
                allowedUserIds.UnionWith(metadata.StaffIds); // Customer sees Staff
                allowedUserIds.UnionWith(bookedCustomerIds); // ...and other Customers on the tour
                allowedUserIds.Add(userId);
            }

            if (!isAuthorized)
            {
                throw new UnauthorizedAccessException("You are not authorized to view live locations for this schedule.");
            }

            var db = _redis.GetDatabase();

            // Lấy từ nguồn: Redis Set (đang ping trong schedule)
            var redisMembers = await db.SetMembersAsync($"schedule_live_{scheduleId}");
            var redisUserIds = redisMembers
                .Select(m => int.TryParse(m.ToString(), out var id) ? id : -1)
                .Where(id => id > 0)
                .ToHashSet();

            // Merge — ưu tiên ai đang ping (Redis), include ai đã book và staff được phân công
            var allUserIds = redisUserIds
                .Union(bookedCustomerIds)
                .Union(metadata.StaffIds)
                .Where(id => allowedUserIds.Contains(id))
                .ToList();

            if (!allUserIds.Any()) return [];

            // Batch lấy tọa độ từ Redis
            var redisKeys = allUserIds
                .Select(id => (RedisKey)$"live_loc_{id}")
                .ToArray();
            var redisValues = await db.StringGetAsync(redisKeys);

            var liveLocations = new List<LiveScheduleMemberLocationDto>();
            for (var i = 0; i < allUserIds.Count; i++)
            {
                if (!redisValues[i].HasValue) continue; // chưa ping thì bỏ qua
                try
                {
                    using var doc = JsonDocument.Parse(redisValues[i].ToString());
                    var root = doc.RootElement;
                    liveLocations.Add(new LiveScheduleMemberLocationDto
                    {
                        UserId = allUserIds[i],
                        Lat = root.GetProperty("lat").GetDouble(),
                        Lng = root.GetProperty("lng").GetDouble(),
                        LastUpdated = root.GetProperty("lastUpdated").GetDateTime()
                    });
                }
                catch { }
            }

            if (!liveLocations.Any()) return [];

            // Lấy profile từ AuthAPI
            var userProfiles = await _authApiClient.GetUserProfilesAsync(
                liveLocations.Select(l => l.UserId));

            foreach (var loc in liveLocations)
            {
                loc.FullName = userProfiles.TryGetValue(loc.UserId, out var p)
                    ? p.FullName ?? "Anonymous"
                    : "Anonymous";
                loc.AvatarUrl = userProfiles.GetValueOrDefault(loc.UserId)?.AvatarUrl;
                loc.Role = metadata.StaffIds.Contains(loc.UserId) ? "Staff" : "Customer";
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

            // 3. Lấy Profile từ AuthAPI (dùng abstraction thay vì hardcode URL)
            var userProfiles = new Dictionary<int, UserProfileShortDto>();
            try
            {
                userProfiles = await _authApiClient.GetUserProfilesAsync(onlineFriendIds);
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
                throw new KeyNotFoundException("User's live location is not available or has expired.");
            }

            double lat = 0;
            double lng = 0;
            DateTime lastUpdated = DateTime.UtcNow;

            try
            {
                using var doc = JsonDocument.Parse(locVal.ToString());
                var root = doc.RootElement;
                lat = root.GetProperty("lat").GetDouble();
                lng = root.GetProperty("lng").GetDouble();
                lastUpdated = root.GetProperty("lastUpdated").GetDateTime();
            }
            catch (Exception)
            {
                throw new KeyNotFoundException("User's live location data is corrupted.");
            }

            string fullName = $"User #{userId}";
            string? avatarUrl = null;

            try
            {
                var userProfiles = await _authApiClient.GetUserProfilesAsync(new List<int> { userId });
                if (userProfiles != null && userProfiles.TryGetValue(userId, out var userProfile))
                {
                    if (!string.IsNullOrWhiteSpace(userProfile.FullName))
                    {
                        fullName = userProfile.FullName;
                    }
                    avatarUrl = userProfile.AvatarUrl;
                }
            }
            catch
            {
                // Fallback to User #{userId} if auth service unavailable
            }

            return new FriendLocationResponseDto
            {
                UserId = userId,
                FullName = fullName,
                AvatarUrl = avatarUrl,
                Lat = lat,
                Lng = lng,
                LastUpdated = lastUpdated
            };
        }

        public async Task<IEnumerable<FootprintDto>> GetMyFootprintsAsync(int userId, int? scheduleId = null)
        {
            try
            {
                // Gom diem theo luoi de "cao map" nhe hon (4 chu so ~ 11m).
                // Tranh tra ve hang ngan diem ping trung nhau khi di chuyen lau.
                const int precision = 4;

                var query = _context.LocationLogs
                    .AsNoTracking()
                    .Where(x => x.UserId == userId);

                if (scheduleId.HasValue && scheduleId.Value > 0)
                {
                    query = query.Where(x => x.ScheduleId == scheduleId.Value);
                }

                var footprints = await query
                    .GroupBy(x => new
                    {
                        LatBucket = Math.Round(x.Lat, precision),
                        LngBucket = Math.Round(x.Lng, precision)
                    })
                    .Select(g => new FootprintDto
                    {
                        Lat = g.Key.LatBucket,
                        Lng = g.Key.LngBucket,
                        // Lan cuoi cung di qua o luoi nay (de sap xep theo thoi gian neu can).
                        Timestamp = g.Max(p => p.Timestamp)
                    })
                    .OrderBy(f => f.Timestamp)
                    .ToListAsync();

                return footprints;
            }
            catch (Exception)
            {
                return new List<FootprintDto>();
            }
        }
        public async Task<IEnumerable<HeatPointDto>> GetHeatmapDataAsync(int? scheduleId, string type, int days)
        {
            try
            {
                const int precision = 3;

                if (type == "moments")
                {
                    var momentsQuery = _context.TourMoments
                        .AsNoTracking()
                        .Where(x => x.Lat.HasValue && x.Lng.HasValue);

                    if (scheduleId.HasValue && scheduleId.Value > 0)
                    {
                        momentsQuery = momentsQuery.Where(x => x.ScheduleId == scheduleId.Value);
                    }

                    var moments = await momentsQuery
                        .GroupBy(x => new
                        {
                            LatBucket = Math.Round(x.Lat.Value, precision),
                            LngBucket = Math.Round(x.Lng.Value, precision)
                        })
                        .Select(g => new HeatPointDto
                        {
                            Lat = g.Key.LatBucket,
                            Lng = g.Key.LngBucket,
                            Weight = g.Sum(x => 1 + x.MomentReactions.Count(r => r.IsLike == true) + x.MomentComments.Count * 2)
                        })
                        .ToListAsync();

                    return moments;
                }
                else
                {
                    // Online users: latest position per active user within last 30 minutes
                    var since = DateTime.UtcNow.AddMinutes(-30);
                    var query = _context.LocationLogs
                        .AsNoTracking()
                        .Where(x => x.Timestamp >= since);

                    if (scheduleId.HasValue && scheduleId.Value > 0)
                    {
                        query = query.Where(x => x.ScheduleId == scheduleId.Value);
                    }

                    var logs = await query.ToListAsync();

                    var points = logs
                        .Where(x => x != null)
                        .GroupBy(x => x.UserId)
                        .Select(g => g.OrderByDescending(x => x.Timestamp).First())
                        .GroupBy(x => new
                        {
                            LatBucket = Math.Round(x.Lat, precision),
                            LngBucket = Math.Round(x.Lng, precision)
                        })
                        .Select(g => new HeatPointDto
                        {
                            Lat = g.Key.LatBucket,
                            Lng = g.Key.LngBucket,
                            Weight = g.Count()
                        })
                        .ToList();

                    // Fallback to historic location logs if no users are online
                    if (!points.Any())
                    {
                        if (days <= 0) days = 90;
                        var fallbackSince = DateTime.UtcNow.AddDays(-days);

                        var fallbackQuery = _context.LocationLogs
                            .AsNoTracking()
                            .Where(x => x.Timestamp >= fallbackSince);

                        if (scheduleId.HasValue && scheduleId.Value > 0)
                        {
                            fallbackQuery = fallbackQuery.Where(x => x.ScheduleId == scheduleId.Value);
                        }

                        points = await fallbackQuery
                            .GroupBy(x => new
                            {
                                LatBucket = Math.Round(x.Lat, precision),
                                LngBucket = Math.Round(x.Lng, precision)
                            })
                            .Select(g => new HeatPointDto
                            {
                                Lat = g.Key.LatBucket,
                                Lng = g.Key.LngBucket,
                                Weight = g.Count()
                            })
                            .ToListAsync();
                    }

                    return points;
                }
            }
            catch (Exception)
            {
                return new List<HeatPointDto>();
            }
        }

        public async Task GoOfflineAsync(int userId)
        {
            try
            {
                var db = _redis.GetDatabase();

                // 1. Delete live position key
                await db.KeyDeleteAsync($"live_loc_{userId}");

                // 2. Notify friends via SignalR
                var friendIds = await _context.Friendships
                    .Where(f => f.Status == "Accepted" && (f.RequesterId == userId || f.ReceiverId == userId))
                    .Select(f => f.RequesterId == userId ? f.ReceiverId : f.RequesterId)
                    .ToListAsync();

                foreach (var friendId in friendIds)
                {
                    await _hubContext.Clients.User(friendId.ToString()).SendAsync("ReceiveUserStoppedSharing", userId);
                }
            }
            catch (Exception)
            {
                // Idempotent and safe: swallow exception to prevent 500
            }
        }

        public async Task StopLocationSharingAsync(int userId)
        {
            try
            {
                // Reuse offline logic to delete live location and notify friends
                await GoOfflineAsync(userId);

                var db = _redis.GetDatabase();

                // Delete active sharing tokens
                var userTokensKey = $"user_tokens_{userId}";
                var activeTokens = await db.SetMembersAsync(userTokensKey);
                if (activeTokens != null && activeTokens.Length > 0)
                {
                    foreach (var tokenVal in activeTokens)
                    {
                        await db.KeyDeleteAsync($"tracking_{tokenVal}");
                    }
                }

                // Delete user tokens collection key
                await db.KeyDeleteAsync(userTokensKey);
            }
            catch (Exception)
            {
                // Ignore exception to keep endpoint safe
            }
        }

        public async Task RevokeTrackingTokenAsync(int currentUserId, string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return;

            try
            {
                var db = _redis.GetDatabase();
                var trackingKey = $"tracking_{token}";
                var userIdVal = await db.StringGetAsync(trackingKey);

                if (userIdVal.HasValue && int.TryParse(userIdVal.ToString(), out int userId))
                {
                    if (userId == currentUserId)
                    {
                        await db.KeyDeleteAsync(trackingKey);
                        await db.SetRemoveAsync($"user_tokens_{currentUserId}", token);
                    }
                }
            }
            catch (Exception)
            {
                // Idempotent and safe: swallow exception to prevent 500
            }
        }
    }
}
