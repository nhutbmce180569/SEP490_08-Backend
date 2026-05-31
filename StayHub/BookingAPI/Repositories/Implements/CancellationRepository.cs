using BookingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingAPI.Repositories.Implements
{
    public class CancellationRepository : ICancellationRepository
    {
        private readonly StayHubBookingDbContext _context;

        public CancellationRepository(StayHubBookingDbContext context)
        {
            _context = context;
        }

        public async Task<Order?> GetOrderForCancellationAsync(int orderId, int customerId)
        {
            return await _context.Orders
                .Include(o => o.CancellationRequests)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);
        }

        public async Task CreateCancellationRequestAsync(CancellationRequest request)
        {
            _context.CancellationRequests.Add(request);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<CancellationRequest>> GetAllCancellationRequestsAsync(string? status)
        {
            var query = _context.CancellationRequests.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            return await query
                .Include(r => r.Order)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<CancellationRequest?> GetCancellationRequestByIdAsync(int id)
        {
            return await _context.CancellationRequests
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task UpdateCancellationRequestAsync(CancellationRequest request)
        {
            _context.CancellationRequests.Update(request);
            await _context.SaveChangesAsync();
        }
    }
}
