using PaymentAPI.Models;

namespace PaymentAPI.Repositories
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetByIdAsync(int id);
        Task<Transaction?> GetByOrderIdAsync(int orderId);
        Task<Transaction> CreateAsync(Transaction transaction);
        Task UpdateAsync(Transaction transaction);
    }
}