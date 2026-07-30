using PaymentAPI.DTOs;

namespace PaymentAPI.Services
{
    public interface IMomoService
    {
        Task<MomoPaymentDTO> CreatePaymentAsync(CreateTransactionDTO transactionDto);
        Task<PaymentCallbackResultDTO> HandleMomoReturnAsync(
            IReadOnlyDictionary<string, string> values);
        Task<bool> ConfirmOrderPaymentAsync(int orderId);
        Task<bool> CancelOrderPaymentAsync(int orderId);
    }
}
