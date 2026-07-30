using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.SignalR;
using SocialAPI.DTOs;
using SocialAPI.Hubs;
using SocialAPI.Repositories;
using SocialAPI.Services;

namespace SocialAPI.Services.Implements
{
    public class ChatNotificationService : IChatNotificationService
    {
        private readonly IChatRepository _chatRepository;
        private readonly IHubContext<NotificationHub> _globalHubContext;
        private readonly IAuthApiClient _authApiClient;
        private readonly ILogger<ChatNotificationService> _logger;

        public ChatNotificationService(
            IChatRepository chatRepository,
            IHubContext<NotificationHub> globalHubContext,
            IAuthApiClient authApiClient,
            ILogger<ChatNotificationService> logger)
        {
            _chatRepository = chatRepository;
            _globalHubContext = globalHubContext;
            _authApiClient = authApiClient;
            _logger = logger;
        }

        public async Task NotifyNewMessageAsync(ChatMessageDto message, int senderId)
        {
            var members = await _chatRepository.GetMembersByRoomIdAsync(message.ChatRoomId);
            var senderName = string.IsNullOrWhiteSpace(message.SenderName)
                ? "Ai đó"
                : message.SenderName;
            var preview = Truncate(message.Content, 120);

            foreach (var member in members)
            {
                if (member.UserId == senderId || member.IsMuted == true)
                    continue;

                await _globalHubContext.Clients.User(member.UserId.ToString())
                    .SendAsync("ReceiveGlobalNotification", message);

                await SendFcmAsync(member.UserId, senderName, preview, message);
            }
        }

        private async Task SendFcmAsync(
            int userId,
            string senderName,
            string preview,
            ChatMessageDto message)
        {
            try
            {
                var messaging = FirebaseMessaging.DefaultInstance;
                var fcmToken = await _authApiClient.GetFcmTokenAsync(userId);

                if (messaging == null || string.IsNullOrEmpty(fcmToken))
                    return;

                var fcmMessage = new Message
                {
                    Token = fcmToken,
                    Notification = new Notification
                    {
                        Title = senderName,
                        Body = preview
                    },
                    Data = new Dictionary<string, string>
                    {
                        ["type"] = "chat",
                        ["chatRoomId"] = message.ChatRoomId.ToString(),
                        ["messageId"] = message.Id.ToString(),
                        ["senderId"] = message.SenderId.ToString(),
                        ["senderName"] = senderName,
                        ["content"] = preview
                    },
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            ChannelId = "stayhub_channel",
                            Sound = "default"
                        }
                    }
                };

                await messaging.SendAsync(fcmMessage);
            }
            catch (FirebaseMessagingException ex)
                when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered
                   || ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
            {
                _logger.LogWarning("FCM token expired for user {UserId}", userId);
                await _authApiClient.ClearFcmTokenAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send chat push notification to user {UserId}", userId);
            }
        }

        private static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Bạn có tin nhắn mới";

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength
                ? trimmed
                : $"{trimmed[..maxLength]}...";
        }
    }
}
