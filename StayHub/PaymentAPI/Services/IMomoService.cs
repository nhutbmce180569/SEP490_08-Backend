using Microsoft.AspNetCore.Http;
using PaymentAPI.DTOs;

namespace PaymentAPI.Services
{
    public interface IMomoService
    {
        Task<string> CreatePaymentUrlAsync(CreateTransactionDTO transactionDto);
        Task<(string Status, string? OrderId)> HandleMomoReturnAsync(IQueryCollection query);
        Task<bool> ConfirmOrderPaymentAsync(int orderId);
        Task<bool> CancelOrderPaymentAsync(int orderId);
    }
}
