using SocialAPI.DTOs;
using SocialAPI.Models;
using SocialAPI.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging; // 💡 ĐÃ BỔ SUNG: Namespace để dùng được ILogger
using SocialAPI.Hubs;
using System.Threading.Tasks;

namespace SocialAPI.Services.Implements
{
    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<ChatService> _logger;
        private readonly IAuthApiClient _authApiClient;
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IBookingApiClient _bookingApiClient;
        private readonly ITourApiClient _tourApiClient;

        public ChatService(
            IChatRepository chatRepository,
            IHttpClientFactory httpClientFactory,
            IHubContext<ChatHub> hubContext,
            ILogger<ChatService> logger,
            IAuthApiClient authApiClient,
            IFriendshipRepository friendshipRepository,
            IBookingApiClient bookingApiClient,
            ITourApiClient tourApiClient)
        {
            _chatRepository = chatRepository;
            _httpClientFactory = httpClientFactory;
            _hubContext = hubContext;
            _logger = logger;
            _authApiClient = authApiClient;
            _friendshipRepository = friendshipRepository;
            _bookingApiClient = bookingApiClient;
            _tourApiClient = tourApiClient;
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
                    userProfiles = await _authApiClient.GetUserProfilesAsync(otherUserIds.Distinct());
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

                int unreadCount = 0;
                if (currentUserMember != null && cr.ChatMessages != null)
                {
                    // Logic: Đếm tin nhắn của người khác gửi sau thời điểm đọc cuối cùng
                    unreadCount = cr.ChatMessages.Count(m => m.SenderId != userId &&
                        (currentUserMember.LastReadAt == null || m.SentAt > currentUserMember.LastReadAt));
                }

                var dto = new ChatRoomDto
                {
                    Id = cr.Id,
                    RoomName = cr.RoomName,
                    IsGroupChat = cr.IsGroupChat ?? false,
                    ScheduleId = cr.ScheduleId,
                    LatestMessage = latestMsg?.Content,
                    LatestMessageTime = latestMsg?.SentAt,
                    IsPinned = currentUserMember?.IsPinned ?? false,
                    IsMuted = currentUserMember?.IsMuted ?? false,
                    UnreadCount = unreadCount
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
                    userProfiles = await _authApiClient.GetUserProfilesAsync(senderIds);
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

                if (cm.SenderId == 0)
                {
                    senderName = "System";
                    senderAvatar = "https://cdn.stayhub.vn/avatars/system.png";
                }
                else if (userProfiles.TryGetValue(cm.SenderId, out var profile))
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

            return messageDtos.OrderBy(m => m.SentAt).ToList();
        }

        public async Task<ChatMessageDto> SaveMessageAsync(int senderId, SendMessageDto dto)
        {
            var isMember = await _chatRepository.IsUserInRoomAsync(dto.ChatRoomId, senderId);
            if (!isMember)
            {
                throw new UnauthorizedAccessException("Sender is not a member of this chat room.");
            }

            var chatRoom = await _chatRepository.GetChatRoomByIdAsync(dto.ChatRoomId);
            if (chatRoom != null && (chatRoom.IsGroupChat == false || chatRoom.IsGroupChat == null))
            {
                var otherMember = chatRoom.ChatMembers.FirstOrDefault(m => m.UserId != senderId);
                if (otherMember != null)
                {
                    int otherUserId = otherMember.UserId;
                    bool isAllowed = await CanChatAsync(senderId, otherUserId);
                    if (!isAllowed)
                    {
                        throw new InvalidOperationException("You do not have permission to chat with this user.");
                    }
                }
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
                var profileDict = await _authApiClient.GetUserProfilesAsync(new List<int> { senderId });
                var profile = profileDict?.Values.FirstOrDefault();
                if (profile != null)
                {
                    senderName = profile.FullName ?? senderName;
                    senderAvatar = profile.AvatarUrl ?? senderAvatar;
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
            bool isAllowed = await CanChatAsync(currentUserId, friendId);
            if (!isAllowed)
            {
                throw new InvalidOperationException("You do not have permission to chat with this user.");
            }

            var room = await _chatRepository.GetDirectChatRoomAsync(currentUserId, friendId);

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

            var addedUserProfiles = await _authApiClient.GetUserProfilesAsync(dto.UserIds);
            var usersList = addedUserProfiles.Values.Select(u => new { id = u.Id, fullName = u.FullName, avatarUrl = u.AvatarUrl }).ToList();
            var contentJson = System.Text.Json.JsonSerializer.Serialize(new { action = "MEMBER_ADDED", users = usersList });

            var systemMessage = new ChatMessage
            {
                ChatRoomId = targetRoomId,
                SenderId = 0,
                Content = contentJson,
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

        public async Task<int> CreateScheduleRoomAsync(CreateScheduleChatRoomRequest dto)
        {
            try
            {
                var existingRoom = await _chatRepository.GetChatRoomByScheduleIdAsync(dto.ScheduleId);

                if (existingRoom != null)
                {
                    return existingRoom.Id;
                }

                var newRoom = await _chatRepository.CreateScheduleChatRoomAsync(dto.ScheduleId, dto.RoomName);
                return newRoom.Id;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error creating schedule chat room: {ex.Message}", ex);
            }
        }

        public async Task AutoAddMemberByScheduleAsync(int scheduleId, AutoAddChatMemberRequest dto)
        {
            try
            {
                var room = await _chatRepository.GetChatRoomByScheduleIdAsync(scheduleId);

                if (room == null)
                {
                    // THAY VÌ BÁO LỖI THÌ TỰ ĐỘNG TẠO LUÔN PHÒNG CHAT CHO SCHEDULE NÀY
                    var roomName = $"Tour Schedule {scheduleId}";
                    room = await _chatRepository.CreateScheduleChatRoomAsync(scheduleId, roomName);
                }

                // Dùng toán tử ?. để đề phòng lỗi NullReference nếu Repository quên Include ChatMembers
                var existingMember = room.ChatMembers?.FirstOrDefault(cm => cm.UserId == dto.UserId);

                if (existingMember != null)
                {
                    return;
                }

                await _chatRepository.AddMembersToGroupAsync(room.Id, new List<int> { dto.UserId });

                var addedUserProfiles = await _authApiClient.GetUserProfilesAsync(new List<int> { dto.UserId });
                var usersList = addedUserProfiles.Values.Select(u => new { id = u.Id, fullName = u.FullName, avatarUrl = u.AvatarUrl }).ToList();
                var contentJson = System.Text.Json.JsonSerializer.Serialize(new { action = "MEMBER_ADDED", users = usersList });

                var systemMessage = new ChatMessage
                {
                    ChatRoomId = room.Id,
                    SenderId = 0,
                    Content = contentJson,
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

                await _hubContext.Clients.Group(room.Id.ToString()).SendAsync("ReceiveMessage", savedMsgDto);

                // QUAN TRỌNG: Gõ cửa Frontend của user để báo hiệu "bạn vừa có một phòng chat mới"
                await _hubContext.Clients.User(dto.UserId.ToString()).SendAsync("NewRoomAdded", room.Id);
            }
            catch (KeyNotFoundException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error adding member to schedule chat room: {ex.Message}", ex);
            }
        }

        public async Task<bool> AddMembersByScheduleAsync(int scheduleId, List<int> userIds)
        {
            try
            {
                var success = await _chatRepository.AddMembersToRoomByScheduleIdAsync(scheduleId, userIds);

                if (success)
                {
                    var room = await _chatRepository.GetChatRoomByScheduleIdAsync(scheduleId);
                    if (room != null)
                    {
                        foreach (var userId in userIds)
                        {
                            await _hubContext.Clients.User(userId.ToString()).SendAsync("NewRoomAdded", room.Id);
                        }
                    }
                }
                return success;
            }
            catch (Exception ex)
            {
                // 💡 ĐÃ HẾT LỖI: Trường _logger hiện tại đã tồn tại trong context và hoạt động hoàn hảo
                _logger.LogError(ex, $"Error adding members to schedule chat room for schedule {scheduleId}");
                return false;
            }
        }

        public async Task<List<UserProfileShortDto>> GetRoomMembersAsync(int roomId)
        {
            try
            {
                // Step 1: Get all ChatMembers for this roomId
                var members = await _chatRepository.GetMembersByRoomIdAsync(roomId);

                if (!members.Any())
                {
                    return new List<UserProfileShortDto>();
                }

                // Step 2: Extract UserIds from members
                var userIds = members.Select(m => m.UserId).Distinct().ToList();

                // Step 3: Fetch user profiles via Gateway
                var userProfiles = new List<UserProfileShortDto>();

                if (userIds.Any())
                {
                    try
                    {
                        var profileDict = await _authApiClient.GetUserProfilesAsync(userIds);
                        if (profileDict != null)
                        {
                            userProfiles = profileDict.Values.ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error fetching user profiles for room members via IAuthApiClient");
                    }
                }

                // Step 4: Return user profile list
                return userProfiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting members for room {roomId}");
                throw;
            }
        }

        public async Task MarkRoomAsReadAsync(int userId, int roomId)
        {
            var room = await _chatRepository.GetChatRoomByIdAsync(roomId);
            if (room == null) throw new KeyNotFoundException("Chat room not found.");

            var member = room.ChatMembers?.FirstOrDefault(cm => cm.UserId == userId);
            if (member == null) throw new UnauthorizedAccessException("You are not a member of this chat room.");

            var latestMessageTime = await _chatRepository.GetLatestMessageSentAtAsync(roomId);
            member.LastReadAt = latestMessageTime ?? DateTime.UtcNow;

            await _chatRepository.UpdateMemberAsync(member);
        }

        public async Task<bool> RemoveMemberByScheduleAsync(int scheduleId, int userId)
        {
            try
            {
                var room = await _chatRepository.GetChatRoomByScheduleIdAsync(scheduleId);
                if (room == null)
                {
                    return false;
                }

                var isMember = room.ChatMembers?.Any(cm => cm.UserId == userId) ?? false;
                if (!isMember)
                {
                    isMember = await _chatRepository.IsUserInRoomAsync(room.Id, userId);
                }

                if (!isMember)
                {
                    return false;
                }

                await _chatRepository.LeaveRoomAsync(room.Id, userId);

                var systemMessage = new ChatMessage
                {
                    ChatRoomId = room.Id,
                    SenderId = 0,
                    Content = "A member has been removed from the team.",
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

                await _hubContext.Clients.Group(room.Id.ToString()).SendAsync("ReceiveMessage", savedMsgDto);
                await _hubContext.Clients.User(userId.ToString()).SendAsync("RoomRemoved", room.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing member {userId} from schedule chat room for schedule {scheduleId}");
                return false;
            }
        }

        private async Task<bool> CanChatAsync(int userId1, int userId2)
        {
            if (userId1 == userId2) return true;

            try
            {
                var profiles = await _authApiClient.GetUserProfilesAsync(new List<int> { userId1, userId2 });
                if (profiles == null || !profiles.TryGetValue(userId1, out var profile1) || !profiles.TryGetValue(userId2, out var profile2))
                {
                    return await _friendshipRepository.CheckAreFriendsAsync(userId1, userId2);
                }

                // If either user is Admin or Manager, allow chat
                bool isUser1AdminOrManager = profile1.RoleNames.Any(r => 
                    r.Equals("Admin", StringComparison.OrdinalIgnoreCase) || 
                    r.Equals("Manager", StringComparison.OrdinalIgnoreCase));
                bool isUser2AdminOrManager = profile2.RoleNames.Any(r => 
                    r.Equals("Admin", StringComparison.OrdinalIgnoreCase) || 
                    r.Equals("Manager", StringComparison.OrdinalIgnoreCase));
                if (isUser1AdminOrManager || isUser2AdminOrManager)
                {
                    return true;
                }

                // Check if either is Staff
                bool isUser1Staff = profile1.RoleNames.Any(r => r.Equals("Staff", StringComparison.OrdinalIgnoreCase));
                bool isUser2Staff = profile2.RoleNames.Any(r => r.Equals("Staff", StringComparison.OrdinalIgnoreCase));

                // If both are staff, allow chat
                if (isUser1Staff && isUser2Staff)
                {
                    return true;
                }

                // If one is customer and one is staff
                if (isUser1Staff || isUser2Staff)
                {
                    int customerId = isUser1Staff ? userId2 : userId1;
                    int staffId = isUser1Staff ? userId1 : userId2;
                    
                    var scheduleIds = new List<int>();

                    // Fetch assigned schedules for Staff
                    var staffSchedules = await _tourApiClient.GetStaffScheduleIdsAsync(staffId);
                    if (staffSchedules != null && staffSchedules.Any())
                    {
                        scheduleIds.AddRange(staffSchedules);
                    }

                    if (!scheduleIds.Any())
                    {
                        return false;
                    }

                    // Check completed bookings
                    return await _bookingApiClient.CheckCompletedBookingAsync(customerId, scheduleIds);
                }

                // If both are regular customers, check friendship
                return await _friendshipRepository.CheckAreFriendsAsync(userId1, userId2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating chat permissions between {User1} and {User2}", userId1, userId2);
                // Fallback to friendship check
                return await _friendshipRepository.CheckAreFriendsAsync(userId1, userId2);
            }
        }
    }
}