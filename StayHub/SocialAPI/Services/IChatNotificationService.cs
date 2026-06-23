using SocialAPI.DTOs;

namespace SocialAPI.Services
{
    public interface IChatNotificationService
    {
        Task NotifyNewMessageAsync(ChatMessageDto message, int senderId);
    }
}
