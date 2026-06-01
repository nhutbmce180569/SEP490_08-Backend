using SocialAPI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocialAPI.Services
{
    public interface IChatService
    {
        Task<List<ChatRoomDto>> GetUserChatRoomsAsync(int userId);
        Task<List<ChatMessageDto>> GetChatHistoryAsync(int roomId, int userId, int skip = 0, int top = 20);
        Task<ChatMessageDto> SaveMessageAsync(int senderId, SendMessageDto dto);
        Task<ChatRoomDto> CreateOrGetChatRoomAsync(int currentUserId, int friendId);
        Task<bool> TogglePinChatAsync(int roomId, int userId);
        Task<bool> ToggleMuteChatAsync(int roomId, int userId);
        Task<ChatRoomDto> AddMembersToRoomAsync(int currentRoomId, int currentUserId, AddMembersRequest dto);
        Task LeaveRoomAsync(int roomId, int userId);
    }
}
