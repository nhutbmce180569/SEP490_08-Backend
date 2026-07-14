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

namespace SocialAPI.Services.Implements;

public class FriendshipService : IFriendshipService
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IMapper _mapper;
    private readonly HttpClient _httpClient;
    private readonly IHubContext<FriendshipHub> _hubContext;
    private readonly string _authApiBase;

    public FriendshipService(IFriendshipRepository friendshipRepository, IMapper mapper, HttpClient httpClient, IHubContext<FriendshipHub> hubContext, IConfiguration configuration)
    {
        _friendshipRepository = friendshipRepository;
        _mapper = mapper;
        _httpClient = httpClient;
        _hubContext = hubContext;
        _authApiBase = configuration["InternalApi:AuthApiBaseUrl"] ?? "https://localhost:7001";
    }

    public async Task<FriendshipResponseDto> SendRequestAsync(int requesterId, FriendRequestDto requestDto)
    {
        if (requesterId == requestDto.ReceiverId)
            throw new InvalidOperationException("You cannot send a friend request to yourself.");

        bool exists = await _friendshipRepository.CheckExistingFriendship(requesterId, requestDto.ReceiverId);
        if (exists)
            throw new InvalidOperationException("A friendship or pending request already exists between these users.");

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
            var response = await _httpClient.PostAsJsonAsync($"{_authApiBase}/api/users/batch", friendIds);
            if (response.IsSuccessStatusCode)
            {
                var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserProfileShortDto>>>();
                if (apiResult?.Data != null)
                {
                    usersDict = apiResult.Data.ToDictionary(u => u.Id, u => u);
                }
            }
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
            var response = await _httpClient.PostAsJsonAsync($"{_authApiBase}/api/users/batch", requesterIds);
            if (response.IsSuccessStatusCode)
            {
                var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserProfileShortDto>>>();
                if (apiResult?.Data != null)
                {
                    usersDict = apiResult.Data.ToDictionary(u => u.Id, u => u);
                }
            }
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
            var response = await _httpClient.PostAsJsonAsync($"{_authApiBase}/api/users/batch", receiverIds);
            if (response.IsSuccessStatusCode)
            {
                var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserProfileShortDto>>>();
                if (apiResult?.Data != null)
                {
                    usersDict = apiResult.Data.ToDictionary(u => u.Id, u => u);
                }
            }
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

        int otherUserId = friendship.RequesterId == userId ? friendship.ReceiverId : friendship.RequesterId;

        await _friendshipRepository.DeleteAsync(friendshipId);

        await _hubContext.Clients.User(otherUserId.ToString()).SendAsync("FriendshipDeleted", userId);
    }

    public async Task<PaginationDTO<FriendshipResponseDto>> GetFriendListAsync(int userId, int page, int pageSize)
    {
        var (friends, total) = await _friendshipRepository.GetFriendListPagedAsync(userId, page, pageSize);

        if (total == 0) return new PaginationDTO<FriendshipResponseDto> { Data = new List<FriendshipResponseDto>(), Total = 0 };

        var friendIds = friends.Select(f => f.RequesterId == userId ? f.ReceiverId : f.RequesterId).Distinct().ToList();

        var authApiUrl = $"{_authApiBase}/api/users/batch";
        var userProfiles = new Dictionary<int, UserProfileShortDto>();
        try
        {
            var response = await _httpClient.PostAsJsonAsync(authApiUrl, friendIds);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserProfileShortDto>>>();
                if (result?.Data != null)
                {
                    userProfiles = result.Data.ToDictionary(u => u.Id, u => u);
                }
            }
        }
        catch
        {
            // Intentionally swallowed: return friend list without profile names if AuthAPI is unavailable
        }

        var friendDtos = _mapper.Map<List<FriendshipResponseDto>>(friends, opt => 
        {
            opt.Items["CurrentUserId"] = userId; 
        });

        foreach (var dto in friendDtos)
        {
            if (userProfiles.TryGetValue(dto.FriendId, out var profile))
            {
                dto.FullName = profile.FullName;
                dto.AvatarUrl = profile.AvatarUrl;
            }
            else
            {
                dto.FullName = "Anonymous user";
            }
        }

        return new PaginationDTO<FriendshipResponseDto>
        {
            Data = friendDtos,
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
            var response = await _httpClient.PostAsJsonAsync($"{_authApiBase}/api/users/batch", new List<int> { dto.FriendId });
            if (response.IsSuccessStatusCode)
            {
                var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserProfileShortDto>>>();
                var userProfile = apiResult?.Data?.FirstOrDefault();
                if (userProfile != null)
                {
                    dto.FullName = userProfile.FullName ?? "Anonymous user";
                    dto.AvatarUrl = userProfile.AvatarUrl;
                }
                else
                {
                    dto.FullName = "Anonymous user";
                }
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
}
