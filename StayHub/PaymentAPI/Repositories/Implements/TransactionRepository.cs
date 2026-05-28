using Microsoft.EntityFrameworkCore;
using PaymentAPI.Models;

namespace PaymentAPI.Repositories.Implements
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly StayHubPaymentDbContext _context; // THAY DB CONTEXT CỦA BẠN VÀO ĐÂY (Vd: PaymentDbContext)

        public TransactionRepository(StayHubPaymentDbContext context)
        {
            _context = context;
        }

        public async Task<Transaction?> GetByIdAsync(int id)
        {
            // Tùy chỉnh DbSet<Transaction> trong Context của bạn
            return await _context.Set<Transaction>().FindAsync(id);
        }

        public async Task<Transaction?> GetByOrderIdAsync(int orderId)
        {
            return await _context.Set<Transaction>()
                .Where(t => t.OrderId == orderId)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<Transaction> CreateAsync(Transaction transaction)
        {
            _context.Set<Transaction>().Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task UpdateAsync(Transaction transaction)
        {
            _context.Set<Transaction>().Update(transaction);
            await _context.SaveChangesAsync();
        }
    }
}