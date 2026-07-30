using SocialAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocialAPI.Repositories
{
    public interface IChatRepository
    {
        Task<List<ChatRoom>> GetUserChatRoomsAsync(int userId);
        Task<bool> IsUserInRoomAsync(int roomId, int userId);
        Task<List<ChatMessage>> GetChatHistoryAsync(int roomId, int skip, int top);
        Task<ChatMessage> SaveMessageAsync(ChatMessage message);
        Task<ChatRoom?> GetDirectChatRoomAsync(int userId1, int userId2);
        Task<ChatRoom> CreateDirectChatRoomAsync(int userId1, int userId2);
        Task<bool> TogglePinAsync(int roomId, int userId);
        Task<bool> ToggleMuteAsync(int roomId, int userId);
        Task<ChatRoom> CreateGroupChatAsync(string roomName, List<int> memberIds);
        Task<ChatRoom?> GetChatRoomByIdAsync(int roomId);
        Task AddMembersToGroupAsync(int roomId, List<int> memberIds);
        Task LeaveRoomAsync(int roomId, int userId);
        Task<ChatRoom?> GetChatRoomByScheduleIdAsync(int scheduleId);
        Task<ChatRoom> CreateScheduleChatRoomAsync(int scheduleId, string roomName);
        Task<bool> AddMembersToRoomByScheduleIdAsync(int scheduleId, List<int> memberIds);
        Task<List<ChatMember>> GetMembersByRoomIdAsync(int roomId);
        Task<DateTime?> GetLatestMessageSentAtAsync(int roomId);
        Task UpdateMemberAsync(ChatMember member);
    }
}