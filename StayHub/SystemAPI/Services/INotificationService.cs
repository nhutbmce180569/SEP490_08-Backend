using SystemAPI.DTOs;

namespace SystemAPI.Services
{
    public interface INotificationService
    {
        Task<ReadNotificationDTO> CreateAndSendNotificationAsync(CreateNotificationDTO dto);
        Task<IEnumerable<ReadNotificationDTO>> GetUserNotificationsAsync(int userId);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task DeleteNotificationAsync(int id, int userId);
    }
}
