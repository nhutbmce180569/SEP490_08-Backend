using SystemAPI.DTOs;

namespace SystemAPI.Services
{
    public interface INotificationService
    {
        Task<ReadNotificationDTO> CreateAndSendNotificationAsync(CreateNotificationDTO dto);
        Task<PaginationDTO<ReadNotificationDTO>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 10);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task DeleteNotificationAsync(int id, int userId);
    }
}
