namespace PaymentAPI.Services
{
    public interface IBookingApiClient
    {
        Task<bool> MarkOrderPaidAsync(int orderId);
        Task<bool> CancelOrderAsync(int orderId);
    }
}
