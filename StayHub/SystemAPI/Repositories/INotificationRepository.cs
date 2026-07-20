using SystemAPI.Models;

namespace SystemAPI.Repositories
{
    public interface INotificationRepository
    {
        Task<Notification> AddAsync(Notification notification);
        Task<(IEnumerable<Notification> items, int totalCount)> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 10);
        Task<Notification?> GetByIdAsync(int id);
        Task UpdateAsync(Notification notification);
        Task DeleteAsync(Notification notification);
    }
}
