using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using SocialAPI.Hubs;
using SocialAPI.DTOs;
using SocialAPI.Models;
using SocialAPI.Repositories;
using Microsoft.EntityFrameworkCore;

namespace SocialAPI.Services.Implements;

public class FriendshipService : IFriendshipService
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IMapper _mapper;
    private readonly HttpClient _httpClient;
    private readonly IHubContext<FriendshipHub> _hubContext;
    private readonly IAuthApiClient _authApiClient;
    private readonly IBookingApiClient _bookingApiClient;
    private readonly ITourApiClient _tourApiClient;
    private readonly StayHubSocialDbContext _context;
    private readonly string _authApiBase;

    public FriendshipService(IFriendshipRepository friendshipRepository, IMapper mapper, HttpClient httpClient, IHubContext<FriendshipHub> hubContext, IConfiguration configuration, IAuthApiClient authApiClient, IBookingApiClient bookingApiClient, ITourApiClient tourApiClient, StayHubSocialDbContext context)
    {
        _friendshipRepository = friendshipRepository;
        _mapper = mapper;
        _httpClient = httpClient;
        _hubContext = hubContext;
        _authApiClient = authApiClient;
        _bookingApiClient = bookingApiClient;
        _tourApiClient = tourApiClient;
        _context = context;
        _authApiBase = configuration["InternalApi:AuthApiBaseUrl"] ?? "https://localhost:7001";
    }

    public async Task<FriendshipResponseDto> SendRequestAsync(int requesterId, FriendRequestDto requestDto)
    {
        if (requesterId == requestDto.ReceiverId)
            throw new InvalidOperationException("You cannot send a friend request to yourself.");

        bool exists = await _friendshipRepository.CheckExistingFriendship(requesterId, requestDto.ReceiverId);
        if (exists)
            throw new InvalidOperationException("A friendship or pending request already exists between these users.");

        // Anti-spam: Check if the receiver has declined a request from the requester within the last 7 days
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        bool recentlyDeclined = await _context.Friendships.AnyAsync(f => 
            f.RequesterId == requesterId && 
            f.ReceiverId == requestDto.ReceiverId && 
            f.Status == "Declined" &&
            f.CreatedAt >= sevenDaysAgo);
        
        if (recentlyDeclined)
            throw new InvalidOperationException("RecentlyDeclinedFriendRequest");

        // Block friend requests involving Staff or Manager accounts.
        // Uses IAuthApiClient (internal /api/users/batch) which returns full RoleNames.
        try
        {
            var profiles = await _authApiClient.GetUserProfilesAsync(new List<int> { requesterId, requestDto.ReceiverId });
            if (profiles.TryGetValue(requesterId, out var requesterProfile))
            {
                var isRestrictedRequester = requesterProfile.RoleNames.Any(r =>
                    r.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                    r.Equals("Manager", StringComparison.OrdinalIgnoreCase));

                if (isRestrictedRequester)
                {
                    throw new InvalidOperationException("You cannot send friend requests if you are a staff or manager.");
                }
            }
            if (profiles.TryGetValue(requestDto.ReceiverId, out var targetProfile))
            {
                var isRestrictedTarget = targetProfile.RoleNames.Any(r =>
                    r.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                    r.Equals("Manager", StringComparison.OrdinalIgnoreCase));

                if (isRestrictedTarget)
                {
                    throw new InvalidOperationException("You cannot send friend requests to staff or manager accounts.");
                }
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch
        {
            // Swallowed: do not block the request if AuthAPI is transiently unavailable
        }

        var friendship = _mapper.Map<Friendship>(requestDto);
        friendship.RequesterId = requesterId;
        friendship.Status = "Pending";
        friendship.CreatedAt = DateTime.UtcNow;

        var created = await _friendshipRepository.AddAsync(friendship);
        
        var response = _mapper.Map<FriendshipResponseDto>(created, opt => {
            opt.Items["CurrentUserId"] = requesterId;
        });
        
        await _hubContext.Clients.User(requestDto.ReceiverId.ToString()).SendAsync("ReceiveFriendRequest", response);

        return response;
    }

    public async Task<IEnumerable<FriendshipResponseDto>> GetFriendshipsAsync(int userId)
    {
        var friendships = await _friendshipRepository.GetFriendshipsByUserIdAsync(userId);
        if (!friendships.Any()) return new List<FriendshipResponseDto>();

        var friendIds = friendships.Select(f => f.RequesterId == userId ? f.ReceiverId : f.RequesterId).Distinct().ToList();
        var usersDict = new Dictionary<int, UserProfileShortDto>();

        try
        {
            var profilesMap = await _authApiClient.GetUserProfilesAsync(friendIds);
            usersDict = profilesMap;
        }
        catch 
        { 
            // Intentionally swallowed to allow returning the list even if the user service is unavailable
        }

        return friendships.Select(f => {
            var dto = _mapper.Map<FriendshipResponseDto>(f, opt => {
                opt.Items["CurrentUserId"] = userId;
            });

            if (usersDict.TryGetValue(dto.FriendId, out var userProfile))
            {
                dto.FullName = userProfile.FullName ?? "Anonymous user";
                dto.AvatarUrl = userProfile.AvatarUrl;
                dto.Email = userProfile.Email;
            }
            else
            {
                dto.FullName = "Anonymous user";
            }

            return dto;
        }).ToList();
    }

    public async Task<IEnumerable<FriendshipResponseDto>> GetPendingRequestsAsync(int userId)
    {
        var requests = await _friendshipRepository.GetPendingRequestsAsync(userId);
        if (!requests.Any()) return new List<FriendshipResponseDto>();

        var requesterIds = requests.Select(r => r.RequesterId).Distinct().ToList();
        var usersDict = new Dictionary<int, UserProfileShortDto>();

        try
        {
            var profilesMap = await _authApiClient.GetUserProfilesAsync(requesterIds);
            usersDict = profilesMap;
        }
        catch
        {
            // Intentionally swallowed to allow returning the pending requests even if the user service is unavailable
        }

        return requests.Select(f =>
        {
            var dto = _mapper.Map<FriendshipResponseDto>(f, opt => {
                opt.Items["CurrentUserId"] = userId;
            });

            if (usersDict.TryGetValue(f.RequesterId, out var userProfile))
            {
                dto.FullName = userProfile.FullName ?? "Anonymous user";
                dto.AvatarUrl = userProfile.AvatarUrl;
                dto.Email = userProfile.Email;
            }
            else
            {
                dto.FullName = "Anonymous user";
            }

            return dto;
        }).ToList();
    }

    public async Task<IEnumerable<FriendshipResponseDto>> GetSentRequestsAsync(int userId)
    {
        var requests = await _friendshipRepository.GetSentRequestsAsync(userId);
        if (!requests.Any()) return new List<FriendshipResponseDto>();

        var receiverIds = requests.Select(r => r.ReceiverId).Distinct().ToList();
        var usersDict = new Dictionary<int, UserProfileShortDto>();

        try
        {
            var profilesMap = await _authApiClient.GetUserProfilesAsync(receiverIds);
            usersDict = profilesMap;
        }
        catch
        {
            // Intentionally swallowed
        }

        return requests.Select(f =>
        {
            var dto = _mapper.Map<FriendshipResponseDto>(f, opt => {
                opt.Items["CurrentUserId"] = userId;
            });

            if (usersDict.TryGetValue(f.ReceiverId, out var userProfile))
            {
                dto.FullName = userProfile.FullName ?? "Anonymous user";
                dto.AvatarUrl = userProfile.AvatarUrl;
                dto.Email = userProfile.Email;
            }
            else
            {
                dto.FullName = "Anonymous user";
            }

            return dto;
        }).ToList();
    }

    public async Task UpdateRequestStatusAsync(int userId, FriendRequestUpdateDto updateDto)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(updateDto.RequestId);
        if (friendship == null)
            throw new KeyNotFoundException("Friend request not found.");

        if (friendship.ReceiverId != userId)
            throw new UnauthorizedAccessException("Only the receiver can accept or reject the friend request.");

        // Whitelist guard: chỉ cho phép các giá trị hợp lệ, đề phòng pipeline bypass validator
        var allowedStatuses = new[] { "Accepted", "Declined" };
        if (!allowedStatuses.Contains(updateDto.Status, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid status value. Allowed: 'Accepted', 'Declined'.");

        friendship.Status = updateDto.Status;
        await _friendshipRepository.UpdateAsync(friendship);

        await _hubContext.Clients.User(friendship.RequesterId.ToString()).SendAsync("FriendRequestResponded", userId, updateDto.Status);
    }

    public async Task DeleteFriendshipAsync(int userId, int friendshipId)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(friendshipId);
        if (friendship == null)
            throw new KeyNotFoundException("Friendship not found.");

        if (friendship.RequesterId != userId && friendship.ReceiverId != userId)
            throw new UnauthorizedAccessException("You do not have permission to delete this friendship.");

        if (!string.Equals(friendship.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("CannotUnfriendUnacceptedFriendship");

        int otherUserId = friendship.RequesterId == userId ? friendship.ReceiverId : friendship.RequesterId;

        await _friendshipRepository.DeleteAsync(friendshipId);

        await _hubContext.Clients.User(otherUserId.ToString()).SendAsync("FriendshipDeleted", userId);
    }

    public async Task CancelRequestAsync(int requesterId, int friendshipId)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(friendshipId);
        if (friendship == null)
            throw new KeyNotFoundException("Friend request not found.");

        if (friendship.RequesterId != requesterId)
            throw new UnauthorizedAccessException("You can only cancel your own friend requests.");

        if (!string.Equals(friendship.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("CannotCancelNonPendingRequest");

        await _friendshipRepository.DeleteAsync(friendshipId);
    }

    public async Task<PaginationDTO<FriendshipResponseDto>> GetFriendListAsync(int userId, int page, int pageSize)
    {
        var allFriends = await _friendshipRepository.GetAllFriendsAsync(userId);
        var total = allFriends.Count;

        if (total == 0) return new PaginationDTO<FriendshipResponseDto> { Data = new List<FriendshipResponseDto>(), Total = 0 };

        var friendIds = allFriends.Select(f => f.RequesterId == userId ? f.ReceiverId : f.RequesterId).Distinct().ToList();

        var userProfiles = new Dictionary<int, UserProfileShortDto>();
        try
        {
            var profilesMap = await _authApiClient.GetUserProfilesAsync(friendIds);
            userProfiles = profilesMap;
        }
        catch
        {
            // Intentionally swallowed: return friend list without profile names if AuthAPI is unavailable
        }

        var friendDtos = _mapper.Map<List<FriendshipResponseDto>>(allFriends, opt => 
        {
            opt.Items["CurrentUserId"] = userId; 
        });

        foreach (var dto in friendDtos)
        {
            if (userProfiles.TryGetValue(dto.FriendId, out var profile))
            {
                dto.FullName = profile.FullName ?? "Anonymous user";
                dto.AvatarUrl = profile.AvatarUrl;
                dto.Email = profile.Email;
            }
            else
            {
                dto.FullName = "Anonymous user";
            }
        }

        // Sort by FullName alphabetically
        friendDtos = friendDtos.OrderBy(d => d.FullName).ToList();

        // Paginate in-memory
        var pagedDtos = friendDtos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PaginationDTO<FriendshipResponseDto>
        {
            Data = pagedDtos,
            Total = total
        };
    }

    public async Task<FriendshipResponseDto?> GetFriendshipStatusAsync(int userId, int targetUserId)
    {
        var friendship = await _friendshipRepository.GetFriendshipBetweenUsersAsync(userId, targetUserId);
        if (friendship == null) return null;

        var dto = _mapper.Map<FriendshipResponseDto>(friendship, opt => {
            opt.Items["CurrentUserId"] = userId;
        });

        try
        {
            var profilesMap = await _authApiClient.GetUserProfilesAsync(new List<int> { dto.FriendId });
            if (profilesMap.TryGetValue(dto.FriendId, out var userProfile))
            {
                dto.FullName = userProfile.FullName ?? "Anonymous user";
                dto.AvatarUrl = userProfile.AvatarUrl;
                dto.Email = userProfile.Email;
            }
            else
            {
                dto.FullName = "Anonymous user";
            }
        }
        catch
        {
            dto.FullName = "Anonymous user";
        }

        return dto;
    }

    public async Task<IEnumerable<UserProfileShortDto>> GetSuggestionsAsync(int userId)
    {
        var existingFriendships = await _friendshipRepository.GetFriendshipsByUserIdAsync(userId);
        var pendingRequests = await _friendshipRepository.GetPendingRequestsAsync(userId);
        var sentRequests = await _friendshipRepository.GetSentRequestsAsync(userId);

        var excludedUserIds = new HashSet<int> { userId };
        foreach (var f in existingFriendships)
        {
            excludedUserIds.Add(f.RequesterId == userId ? f.ReceiverId : f.RequesterId);
        }
        foreach (var r in pendingRequests)
        {
            excludedUserIds.Add(r.RequesterId);
        }
        foreach (var r in sentRequests)
        {
            excludedUserIds.Add(r.ReceiverId);
        }

        // 1. Friends of Friends suggestions
        var friendIds = existingFriendships
            .Select(f => f.RequesterId == userId ? f.ReceiverId : f.RequesterId)
            .ToList();

        var friendsOfFriendsIds = new List<int>();
        if (friendIds.Any())
        {
            foreach (var fId in friendIds)
            {
                var fFriendships = await _friendshipRepository.GetFriendshipsByUserIdAsync(fId);
                foreach (var ff in fFriendships)
                {
                    int potentialId = ff.RequesterId == fId ? ff.ReceiverId : ff.RequesterId;
                    if (!excludedUserIds.Contains(potentialId))
                    {
                        friendsOfFriendsIds.Add(potentialId);
                    }
                }
            }
        }

        // 2. Shared Tour Participant suggestions
        var scheduleIds = await _bookingApiClient.GetEligibleScheduleIdsByUserAsync(userId);
        var tourUserIds = new List<int>();
        if (scheduleIds != null && scheduleIds.Any())
        {
            foreach (var sId in scheduleIds)
            {
                var customerIds = await _bookingApiClient.GetCustomerIdsByScheduleAsync(sId);
                if (customerIds != null)
                {
                    foreach (var cId in customerIds)
                    {
                        if (!excludedUserIds.Contains(cId))
                        {
                            tourUserIds.Add(cId);
                        }
                    }
                }
            }
        }

        // Rank suggestions by mutual context frequency
        var suggestionsMap = new Dictionary<int, int>();
        foreach (var id in friendsOfFriendsIds)
        {
            if (!suggestionsMap.ContainsKey(id)) suggestionsMap[id] = 0;
            suggestionsMap[id] += 2; // Mutual friend weight
        }
        foreach (var id in tourUserIds)
        {
            if (!suggestionsMap.ContainsKey(id)) suggestionsMap[id] = 0;
            suggestionsMap[id] += 1; // Shared tour weight
        }

        var topSuggestedIds = suggestionsMap
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .Take(15)
            .ToList();

        if (topSuggestedIds.Count < 5)
        {
            var fallbackIds = new HashSet<int>();
            try
            {
                var activeMomentUserIds = await _context.TourMoments
                    .Select(m => m.UserId)
                    .Distinct()
                    .Take(20)
                    .ToListAsync();

                var activeCommentUserIds = await _context.MomentComments
                    .Select(c => c.UserId)
                    .Distinct()
                    .Take(20)
                    .ToListAsync();

                var activeFriendshipUserIds = await _context.Friendships
                    .SelectMany(f => new[] { f.RequesterId, f.ReceiverId })
                    .Distinct()
                    .Take(50)
                    .ToListAsync();

                foreach (var id in activeMomentUserIds) fallbackIds.Add(id);
                foreach (var id in activeCommentUserIds) fallbackIds.Add(id);
                foreach (var id in activeFriendshipUserIds) fallbackIds.Add(id);
            }
            catch
            {
                // Fallback gracefully if context fails
            }

            var validFallbackIds = fallbackIds
                .Where(id => !excludedUserIds.Contains(id) && !topSuggestedIds.Contains(id))
                .Take(15 - topSuggestedIds.Count)
                .ToList();

            topSuggestedIds.AddRange(validFallbackIds);
        }

        var suggestedProfiles = new List<UserProfileShortDto>();
        if (topSuggestedIds.Any())
        {
            var profilesMap = await _authApiClient.GetUserProfilesAsync(topSuggestedIds);
            foreach (var pId in topSuggestedIds)
            {
                if (profilesMap.TryGetValue(pId, out var profile))
                {
                    bool isSystemUser = profile.RoleNames.Any(r =>
                        r.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                        r.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
                        r.Equals("Admin", StringComparison.OrdinalIgnoreCase));

                    if (!isSystemUser)
                    {
                        suggestedProfiles.Add(profile);
                    }
                }
            }
        }

        return suggestedProfiles;
    }
}
