namespace BookingAPI.Services
{
    public interface IBackgroundJobService
    {
        void ScheduleAutoCancelOrder(int orderId);
        Task CancelOrderIfUnpaidAsync(int orderId);
        Task CancelExpiredUnpaidOrdersAsync();
        void EnqueueSendTicketsEmail(int orderId, string customerEmail);
        Task SendTicketsEmailForPaidOrderAsync(int orderId, string customerEmail);
    }
}
