using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using SystemAPI.DTOs;
using SystemAPI.Hubs;
using SystemAPI.Models;
using SystemAPI.Repositories;
using SystemAPI.Repositories.Implements;

namespace SystemAPI.Services.Implements
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly IMapper _mapper;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            INotificationRepository repo,
            IMapper mapper,
            IHubContext<NotificationHub> hubContext)
        {
            _repo = repo;
            _mapper = mapper;
            _hubContext = hubContext;
        }

        public async Task<ReadNotificationDTO> CreateAndSendNotificationAsync(CreateNotificationDTO dto)
        {
            var entity = _mapper.Map<Notification>(dto);
            var savedEntity = await _repo.AddAsync(entity);
            var resultDto = _mapper.Map<ReadNotificationDTO>(savedEntity);

            //Đẩy thông báo qua SignalR đến đúng UserId đó
            await _hubContext.Clients.User(dto.UserId.ToString())
                .SendAsync("ReceiveNewNotification", resultDto);

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

            // 3. KIỂM TRA BẢO MẬT (Authorization): 
            // Nếu User ID của người đang đăng nhập KHÔNG khớp với User ID của thông báo -> Chặn ngay!
            if (notification.UserId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this notification.");
            }

            // 4. Hợp lệ thì tiến hành xóa
            await _repo.DeleteAsync(notification);
        }
    }
}
