namespace PaymentAPI.Services
{
    public interface IBookingApiClient
    {
        Task<bool> MarkOrderPaidAsync(int orderId, string? customerEmail);
        Task<bool> CancelOrderAsync(int orderId);
    }
}
