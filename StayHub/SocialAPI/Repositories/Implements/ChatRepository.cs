using Microsoft.EntityFrameworkCore;
using SocialAPI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocialAPI.Repositories.Implements
{
    public class ChatRepository : IChatRepository
    {
        private readonly StayHubSocialDbContext _context;

        public ChatRepository(StayHubSocialDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChatRoom>> GetUserChatRoomsAsync(int userId)
        {
            // Trả về Entity, xử lý lấy tin nhắn mới nhất
            return await _context.ChatRooms
                .Include(cr => cr.ChatMembers)
                .Include(cr => cr.ChatMessages)
                .Where(cr => cr.ChatMembers.Any(cm => cm.UserId == userId))
                .ToListAsync();
        }

        public async Task<bool> IsUserInRoomAsync(int roomId, int userId)
        {
            return await _context.ChatMembers
                .AnyAsync(cm => cm.ChatRoomId == roomId && cm.UserId == userId);
        }

        public async Task<List<ChatMessage>> GetChatHistoryAsync(int roomId, int skip, int top)
        {
            return await _context.ChatMessages
                .AsNoTracking()
                .Where(cm => cm.ChatRoomId == roomId)
                .OrderByDescending(cm => cm.SentAt)
                .Skip(skip)
                .Take(top)
                .ToListAsync();
        }

        public async Task<ChatMessage> SaveMessageAsync(ChatMessage message)
        {
            await _context.ChatMessages.AddAsync(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<ChatRoom?> GetDirectChatRoomAsync(int userId1, int userId2)
        {
            return await _context.ChatRooms
                .Include(cr => cr.ChatMembers)
                .Include(cr => cr.ChatMessages)
                .Where(cr => cr.IsGroupChat == false &&
                             cr.ChatMembers.Count == 2 &&
                             cr.ChatMembers.Any(cm => cm.UserId == userId1) &&
                             cr.ChatMembers.Any(cm => cm.UserId == userId2))
                .FirstOrDefaultAsync();
        }

        public async Task<ChatRoom> CreateDirectChatRoomAsync(int userId1, int userId2)
        {
            var chatRoom = new ChatRoom
            {
                IsGroupChat = false
            };

            await _context.ChatRooms.AddAsync(chatRoom);

            // Tự động tạo quan hệ (mapping) cho 2 user thông qua ChatMembers
            chatRoom.ChatMembers.Add(new ChatMember { UserId = userId1 });
            chatRoom.ChatMembers.Add(new ChatMember { UserId = userId2 });

            await _context.SaveChangesAsync();
            return chatRoom;
        }

        public async Task<bool> TogglePinAsync(int roomId, int userId)
        {
            var member = await _context.ChatMembers
                .FirstOrDefaultAsync(cm => cm.ChatRoomId == roomId && cm.UserId == userId);

            if (member == null) return false;

            member.IsPinned = !(member.IsPinned ?? false);
            await _context.SaveChangesAsync();
            return member.IsPinned.Value;
        }

        public async Task<bool> ToggleMuteAsync(int roomId, int userId)
        {
            var member = await _context.ChatMembers
                .FirstOrDefaultAsync(cm => cm.ChatRoomId == roomId && cm.UserId == userId);

            if (member == null) return false;

            member.IsMuted = !(member.IsMuted ?? false);
            await _context.SaveChangesAsync();
            return member.IsMuted.Value;
        }

        public async Task<ChatRoom> CreateGroupChatAsync(string roomName, List<int> memberIds)
        {
            var chatRoom = new ChatRoom
            {
                RoomName = roomName,
                IsGroupChat = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ChatRooms.AddAsync(chatRoom);

            foreach (var userId in memberIds.Distinct())
            {
                chatRoom.ChatMembers.Add(new ChatMember { UserId = userId });
            }

            await _context.SaveChangesAsync();
            return chatRoom;
        }

        public async Task<ChatRoom?> GetChatRoomByIdAsync(int roomId)
        {
            return await _context.ChatRooms
                .Include(cr => cr.ChatMembers)
                .FirstOrDefaultAsync(cr => cr.Id == roomId);
        }

        public async Task AddMembersToGroupAsync(int roomId, List<int> memberIds)
        {
            var room = await _context.ChatRooms
                .Include(cr => cr.ChatMembers)
                .FirstOrDefaultAsync(cr => cr.Id == roomId);

            if (room != null)
            {
                var existingMemberIds = room.ChatMembers.Select(cm => cm.UserId).ToList();
                foreach (var userId in memberIds.Distinct())
                {
                    if (!existingMemberIds.Contains(userId))
                    {
                        room.ChatMembers.Add(new ChatMember { UserId = userId, ChatRoomId = roomId });
                    }
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task LeaveRoomAsync(int roomId, int userId)
        {
            var member = await _context.ChatMembers
                .FirstOrDefaultAsync(cm => cm.ChatRoomId == roomId && cm.UserId == userId);

            if (member != null)
            {
                _context.ChatMembers.Remove(member);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<ChatRoom?> GetChatRoomByScheduleIdAsync(int scheduleId)
        {
            return await _context.ChatRooms
                .AsNoTracking()
                .Include(cr => cr.ChatMembers)
                .FirstOrDefaultAsync(cr => cr.ScheduleId == scheduleId);
        }

        public async Task<ChatRoom> CreateScheduleChatRoomAsync(int scheduleId, string roomName)
        {
            var chatRoom = new ChatRoom
            {
                ScheduleId = scheduleId,
                RoomName = roomName,
                IsGroupChat = true,
                CreatedAt = DateTime.UtcNow,
                ChatMembers = new List<ChatMember>()
            };

            await _context.ChatRooms.AddAsync(chatRoom);
            await _context.SaveChangesAsync();
            return chatRoom;
        }

        public async Task<bool> AddMembersToRoomByScheduleIdAsync(int scheduleId, List<int> memberIds)
        {
            var room = await _context.ChatRooms
                .Include(cr => cr.ChatMembers)
                .FirstOrDefaultAsync(cr => cr.ScheduleId == scheduleId);

            if (room == null)
            {
                return false;
            }

            var existingMemberIds = room.ChatMembers.Select(cm => cm.UserId).ToList();
            foreach (var userId in memberIds.Distinct())
            {
                if (!existingMemberIds.Contains(userId))
                {
                    room.ChatMembers.Add(new ChatMember { UserId = userId, ChatRoomId = room.Id });
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ChatMember>> GetMembersByRoomIdAsync(int roomId)
        {
            return await _context.ChatMembers
                .AsNoTracking()
                .Where(cm => cm.ChatRoomId == roomId)
                .ToListAsync();
        }

        public async Task<DateTime?> GetLatestMessageSentAtAsync(int roomId)
        {
            return await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.ChatRoomId == roomId)
                .OrderByDescending(m => m.SentAt)
                .Select(m => m.SentAt)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateMemberAsync(ChatMember member)
        {
            _context.ChatMembers.Update(member);
            await _context.SaveChangesAsync();
        }
    }
}