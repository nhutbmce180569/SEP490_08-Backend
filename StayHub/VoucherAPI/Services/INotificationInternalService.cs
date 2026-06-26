namespace VoucherAPI.Services
{
    public interface INotificationInternalService
    {
        Task NotifyUserAsync(int userId, string title, string content);
    }
}
