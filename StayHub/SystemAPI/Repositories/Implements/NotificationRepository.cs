using Microsoft.EntityFrameworkCore;
using SystemAPI.Models;

namespace SystemAPI.Repositories.Implements
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly StayHubSystemDbContext _context;

        public NotificationRepository(StayHubSystemDbContext context)
        {
            _context = context;
        }

        public async Task<Notification> AddAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<(IEnumerable<Notification> items, int totalCount)> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 10)
        {
            var query = _context.Notifications.Where(n => n.UserId == userId);
            
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task UpdateAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Notification notification)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }
}
