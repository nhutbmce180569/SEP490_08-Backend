using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using SystemAPI.DTOs;
using SystemAPI.Hubs;
using SystemAPI.Repositories;
using SystemAPI.Repositories.Implements;

namespace SystemAPI.Services.Implements
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly IMapper _mapper;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IAuthApiClient _authApiClient;

        public NotificationService(
            INotificationRepository repo,
            IMapper mapper,
            IHubContext<NotificationHub> hubContext,
            IAuthApiClient authApiClient)
        {
            _repo = repo;
            _mapper = mapper;
            _hubContext = hubContext;
            _authApiClient = authApiClient;
        }

        public async Task<ReadNotificationDTO> CreateAndSendNotificationAsync(CreateNotificationDTO dto)
        {
            var entity = _mapper.Map<SystemAPI.Models.Notification>(dto);
            var savedEntity = await _repo.AddAsync(entity);
            var resultDto = _mapper.Map<ReadNotificationDTO>(savedEntity);

            // 1. SignalR
            await _hubContext.Clients.User(dto.UserId.ToString())
                .SendAsync("ReceiveNewNotification", resultDto);

            // 2. FCM Push Notification
            try
            {
                var messaging = FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance;
                string? userFcmToken = await _authApiClient.GetFcmTokenAsync(dto.UserId);

                if (messaging != null && !string.IsNullOrEmpty(userFcmToken))
                {
                    var message = new FirebaseAdmin.Messaging.Message()
                    {
                        Token = userFcmToken,
                        Notification = new FirebaseAdmin.Messaging.Notification()
                        {
                            Title = dto.Title,
                            Body = dto.Content
                        }
                    };

                    await messaging.SendAsync(message);
                }
            }
            catch (FirebaseAdmin.Messaging.FirebaseMessagingException ex)
                when (ex.MessagingErrorCode == FirebaseAdmin.Messaging.MessagingErrorCode.Unregistered
                   || ex.MessagingErrorCode == FirebaseAdmin.Messaging.MessagingErrorCode.InvalidArgument)
            {
                Console.WriteLine($"[FCM] Token hết hạn cho userId {dto.UserId}, tiến hành xóa.");
                await _authApiClient.ClearFcmTokenAsync(dto.UserId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LỖI PUSH NOTIFICATION]: {ex.Message}");
            }

            return resultDto;
        }

        public async Task<IEnumerable<ReadNotificationDTO>> GetUserNotificationsAsync(int userId)
        {
            var notifications = await _repo.GetUserNotificationsAsync(userId);
            return _mapper.Map<IEnumerable<ReadNotificationDTO>>(notifications);
        }

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _repo.GetByIdAsync(notificationId);

            if (notification == null)
                throw new Exception("Notification not found.");

            // Kiểm tra bảo mật: Chỉ cho phép người sở hữu đánh dấu đã đọc
            if (notification.UserId != userId)
                throw new Exception("You do not have permission to modify this notification.");

            notification.IsRead = true;
            await _repo.UpdateAsync(notification);
        }

        public async Task DeleteNotificationAsync(int id, int userId)
        {
            // 1. Tìm thông báo trong Database
            var notification = await _repo.GetByIdAsync(id);

            // 2. Kiểm tra tồn tại
            if (notification == null)
            {
                throw new Exception("Notification not found.");
            }

            if (notification.UserId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this notification.");
            }

            // 4. Hợp lệ thì tiến hành xóa
            await _repo.DeleteAsync(notification);
        }
    }
}
