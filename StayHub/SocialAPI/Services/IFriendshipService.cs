using System.Collections.Generic;
using System.Threading.Tasks;
using SocialAPI.DTOs;

namespace SocialAPI.Services;

public interface IFriendshipService
{
    Task<FriendshipResponseDto> SendRequestAsync(int requesterId, FriendRequestDto requestDto);
    Task<IEnumerable<FriendshipResponseDto>> GetFriendshipsAsync(int userId);
    Task<IEnumerable<FriendshipResponseDto>> GetPendingRequestsAsync(int userId);
    Task UpdateRequestStatusAsync(int userId, FriendRequestUpdateDto updateDto);
    Task DeleteFriendshipAsync(int userId, int friendshipId);
    Task<PaginationDTO<FriendshipResponseDto>> GetFriendListAsync(int userId, int page, int pageSize);
    Task<FriendshipResponseDto?> GetFriendshipStatusAsync(int userId, int targetUserId);
}