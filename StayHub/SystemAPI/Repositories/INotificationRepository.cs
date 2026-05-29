using SystemAPI.Models;

namespace SystemAPI.Repositories
{
    public interface INotificationRepository
    {
        Task<Notification> AddAsync(Notification notification);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId);
        Task<Notification?> GetByIdAsync(int id);
        Task UpdateAsync(Notification notification);
        Task DeleteAsync(Notification notification);
    }
}
