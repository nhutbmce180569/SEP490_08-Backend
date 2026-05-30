using System.Threading.Tasks;

namespace TourAPI.Services
{
    public interface INotificationInternalService
    {
        Task NotifyUserAsync(int userId, string title, string content);
    }
}
