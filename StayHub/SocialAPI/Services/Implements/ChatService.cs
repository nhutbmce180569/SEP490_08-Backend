using SocialAPI.DTOs;
using SocialAPI.Models;
using SocialAPI.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using SocialAPI.Hubs;
using System.Threading.Tasks;

namespace SocialAPI.Services.Implements
{
    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatService(IChatRepository chatRepository, IHttpClientFactory httpClientFactory, IHubContext<ChatHub> hubContext)
        {
            _chatRepository = chatRepository;
            _httpClientFactory = httpClientFactory;
            _hubContext = hubContext;
        }

        public async Task<List<ChatRoomDto>> GetUserChatRoomsAsync(int userId)
        {
            var chatRooms = await _chatRepository.GetUserChatRoomsAsync(userId);

            var directChatRooms = chatRooms.Where(cr => cr.IsGroupChat == false).ToList();
            var otherUserIds = new List<int>();

            foreach (var room in directChatRooms)
            {
                var otherMember = room.ChatMembers.FirstOrDefault(cm => cm.UserId != userId);
                if (otherMember != null)
                {
                    otherUserIds.Add(otherMember.UserId);
                }
            }

            var userProfiles = new Dictionary<int, UserProfileShortDto>();
            if (otherUserIds.Any())
            {
                try
                {
                    using var client = _httpClientFactory.CreateClient();
                    var response = await client.PostAsJsonAsync("https://localhost:7010/api/users/batch", otherUserIds.Distinct());
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
                    // Ignored intentionally, fallback to default naming/avatar
                }
            }

            var roomDtos = chatRooms.Select(cr =>
            {
                var latestMsg = cr.ChatMessages.OrderByDescending(m => m.SentAt).FirstOrDefault();
                var currentUserMember = cr.ChatMembers.FirstOrDefault(cm => cm.UserId == userId);

                var dto = new ChatRoomDto
                {
                    Id = cr.Id,
                    RoomName = cr.RoomName,
                    IsGroupChat = cr.IsGroupChat ?? false,
                    ScheduleId = cr.ScheduleId,
                    LatestMessage = latestMsg?.Content,
                    LatestMessageTime = latestMsg?.SentAt,
                    IsPinned = currentUserMember?.IsPinned ?? false,
                    IsMuted = currentUserMember?.IsMuted ?? false
                };

                if (cr.IsGroupChat == false)
                {
                    var otherMember = cr.ChatMembers.FirstOrDefault(cm => cm.UserId != userId);
                    if (otherMember != null && userProfiles.TryGetValue(otherMember.UserId, out var profile))
                    {
                        dto.RoomName = profile.FullName;
                        dto.AvatarUrl = profile.AvatarUrl;
                    }
                    else
                    {
                        dto.RoomName = "Unknown User";
                        dto.AvatarUrl = "https://cdn.stayhub.vn/avatars/default.png";
                    }
                }

                return dto;
            }).ToList();

            return roomDtos
                .OrderByDescending(x => x.IsPinned)
                .ThenByDescending(x => x.LatestMessageTime ?? DateTime.MinValue)
                .ToList();
        }

        public async Task<List<ChatMessageDto>> GetChatHistoryAsync(int roomId, int userId, int skip = 0, int top = 20)
        {
            // Kiểm tra quyền (Nghiệp vụ)
            var isMember = await _chatRepository.IsUserInRoomAsync(roomId, userId);
            if (!isMember)
            {
                throw new UnauthorizedAccessException("User is not a member of this chat room.");
            }

            var messages = await _chatRepository.GetChatHistoryAsync(roomId, skip, top);

            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
            var userProfiles = new Dictionary<int, UserProfileShortDto>();

            if (senderIds.Any())
            {
                try
                {
                    using var client = _httpClientFactory.CreateClient();
                    var response = await client.PostAsJsonAsync("https://localhost:7010/api/users/batch", senderIds);
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
                    // Ignored intentionally, fallback to default naming/avatar
                }
            }

            var messageDtos = messages.Select(cm =>
            {
                var senderName = "Unknown User";
                var senderAvatar = "https://cdn.stayhub.vn/avatars/default.png";

                if (userProfiles.TryGetValue(cm.SenderId, out var profile))
                {
                    senderName = profile.FullName ?? senderName;
                    senderAvatar = profile.AvatarUrl ?? senderAvatar;
                }

                return new ChatMessageDto
                {
                    Id = cm.Id,
                    ChatRoomId = cm.ChatRoomId,
                    SenderId = cm.SenderId,
                    SenderName = senderName,
                    SenderAvatar = senderAvatar,
                    Content = cm.Content,
                    IsRead = cm.IsRead ?? false,
                    SentAt = cm.SentAt ?? DateTime.UtcNow
                };
            }).ToList();

            // Đảo ngược lại để Frontend render từ trên xuống dưới cho đúng flow chat
            return messageDtos.OrderBy(m => m.SentAt).ToList();
        }

        public async Task<ChatMessageDto> SaveMessageAsync(int senderId, SendMessageDto dto)
        {
            var isMember = await _chatRepository.IsUserInRoomAsync(dto.ChatRoomId, senderId);
            if (!isMember)
            {
                throw new UnauthorizedAccessException("Sender is not a member of this chat room.");
            }

            var chatMessage = new ChatMessage
            {
                ChatRoomId = dto.ChatRoomId,
                SenderId = senderId,
                Content = dto.Content,
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            var savedMsg = await _chatRepository.SaveMessageAsync(chatMessage);

            var senderName = "Unknown User";
            var senderAvatar = "https://cdn.stayhub.vn/avatars/default.png";

            try
            {
                using var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync("https://localhost:7010/api/users/batch", new List<int> { senderId });
                if (response.IsSuccessStatusCode)
                {
                    var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserProfileShortDto>>>();
                    var profile = apiResult?.Data?.FirstOrDefault();
                    if (profile != null)
                    {
                        senderName = profile.FullName ?? senderName;
                        senderAvatar = profile.AvatarUrl ?? senderAvatar;
                    }
                }
            }
            catch
            {
                // Ignored intentionally, fallback to default naming/avatar
            }

            return new ChatMessageDto
            {
                Id = savedMsg.Id,
                ChatRoomId = savedMsg.ChatRoomId,
                SenderId = savedMsg.SenderId,
                SenderName = senderName,
                SenderAvatar = senderAvatar,
                Content = savedMsg.Content,
                IsRead = savedMsg.IsRead ?? false,
                SentAt = savedMsg.SentAt ?? DateTime.UtcNow
            };
        }

        public async Task<ChatRoomDto> CreateOrGetChatRoomAsync(int currentUserId, int friendId)
        {
            // Tìm phòng chat trực tiếp hiện tại
            var room = await _chatRepository.GetDirectChatRoomAsync(currentUserId, friendId);

            // Nếu chưa tồn tại, yêu cầu Repository tạo mới
            if (room == null)
            {
                room = await _chatRepository.CreateDirectChatRoomAsync(currentUserId, friendId);
            }

            var latestMsg = room.ChatMessages?.OrderByDescending(m => m.SentAt).FirstOrDefault();

            return new ChatRoomDto
            {
                Id = room.Id,
                RoomName = room.RoomName,
                IsGroupChat = room.IsGroupChat ?? false,
                ScheduleId = room.ScheduleId,
                LatestMessage = latestMsg?.Content,
                LatestMessageTime = latestMsg?.SentAt
            };
        }

        public async Task<bool> TogglePinChatAsync(int roomId, int userId)
        {
            return await _chatRepository.TogglePinAsync(roomId, userId);
        }

        public async Task<bool> ToggleMuteChatAsync(int roomId, int userId)
        {
            return await _chatRepository.ToggleMuteAsync(roomId, userId);
        }

        public async Task<ChatRoomDto> AddMembersToRoomAsync(int currentRoomId, int currentUserId, AddMembersRequest dto)
        {
            var room = await _chatRepository.GetChatRoomByIdAsync(currentRoomId);
            if (room == null) throw new KeyNotFoundException("Chat room not found.");

            var isMember = room.ChatMembers.Any(cm => cm.UserId == currentUserId);
            if (!isMember) throw new UnauthorizedAccessException("You are not a member of this chat room.");

            int targetRoomId = currentRoomId;
            ChatRoom targetRoom = room;

            if (room.IsGroupChat == false)
            {
                var existingMemberIds = room.ChatMembers.Select(cm => cm.UserId).ToList();
                var allMembers = existingMemberIds.Concat(dto.UserIds).Distinct().ToList();

                targetRoom = await _chatRepository.CreateGroupChatAsync("Group Chat", allMembers);
                targetRoomId = targetRoom.Id;
            }
            else
            {
                await _chatRepository.AddMembersToGroupAsync(currentRoomId, dto.UserIds);
            }

            var systemMessage = new ChatMessage
            {
                ChatRoomId = targetRoomId,
                SenderId = 0,
                Content = "Một thành viên mới đã được thêm vào nhóm",
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            var savedMsg = await _chatRepository.SaveMessageAsync(systemMessage);

            var savedMsgDto = new ChatMessageDto
            {
                Id = savedMsg.Id,
                ChatRoomId = savedMsg.ChatRoomId,
                SenderId = savedMsg.SenderId,
                SenderName = "System",
                SenderAvatar = "https://cdn.stayhub.vn/avatars/system.png",
                Content = savedMsg.Content,
                IsRead = savedMsg.IsRead ?? false,
                SentAt = savedMsg.SentAt ?? DateTime.UtcNow
            };

            await _hubContext.Clients.Group(targetRoomId.ToString()).SendAsync("ReceiveMessage", savedMsgDto);

            return new ChatRoomDto
            {
                Id = targetRoom.Id,
                RoomName = targetRoom.RoomName,
                IsGroupChat = targetRoom.IsGroupChat ?? false,
                ScheduleId = targetRoom.ScheduleId,
                LatestMessage = savedMsg.Content,
                LatestMessageTime = savedMsg.SentAt,
                IsPinned = false,
                IsMuted = false
            };
        }

        public async Task LeaveRoomAsync(int roomId, int userId)
        {
            await _chatRepository.LeaveRoomAsync(roomId, userId);
        }
    }
}