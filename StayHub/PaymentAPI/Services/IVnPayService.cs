using Microsoft.AspNetCore.Http;
using PaymentAPI.DTOs;

namespace PaymentAPI.Services
{
    public interface IVnPayService
    {
        Task<string> CreatePaymentUrl(CreateTransactionDTO transactionDto, string? ipAddress);
        Task<PaymentCallbackResultDTO> HandleVnPayReturnAsync(
            IQueryCollection query,
            string rawQuery);
        Task<bool> ConfirmOrderPaymentAsync(int orderId);
        Task<bool> CancelOrderPaymentAsync(int orderId);
    }
}
