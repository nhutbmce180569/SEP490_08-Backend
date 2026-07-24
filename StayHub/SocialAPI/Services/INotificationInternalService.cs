using System.Threading.Tasks;

namespace SocialAPI.Services
{
    public interface INotificationInternalService
    {
        Task NotifyUserAsync(int userId, string title, string content);
    }
}
