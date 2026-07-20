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

        public async Task<(IEnumerable<CancellationRequest> Data, int Total)> GetAllCancellationRequestsAsync(
            IReadOnlyCollection<int> scheduleIds,
            string? status,
            string? date,
            int page,
            int pageSize)
        {
            var query = _context.CancellationRequests
                .AsNoTracking()
                .Where(r => r.Order != null && scheduleIds.Contains(r.Order.ScheduleId));

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
            {
                query = query.Where(r => r.RequestedAt.HasValue && r.RequestedAt.Value.Date == parsedDate.Date);
            }

            int total = await query.CountAsync();

            var data = await query
                .Include(r => r.Order)
                // 1. Ưu tiên "Pending" lên đầu (Pending = 0, Khác = 1) -> Sắp xếp tăng dần
                .OrderBy(r => r.Status == "Pending" ? 0 : 1)
                // 2. Sau đó mới sắp xếp theo ngày yêu cầu mới nhất
                .ThenByDescending(r => r.RequestedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, total);
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
