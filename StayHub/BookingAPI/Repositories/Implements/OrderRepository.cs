using BookingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingAPI.Repositories.Implements
{
    public class OrderRepository : IOrderRepository
    {
        private readonly StayHubBookingDbContext _context;

        public OrderRepository(StayHubBookingDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasCompletedBookingAsync(int customerId, List<int> scheduleIds)
        {
            if (scheduleIds == null || !scheduleIds.Any()) return false;

            return await _context.Orders.AnyAsync(o =>
                o.CustomerId == customerId &&
                scheduleIds.Contains(o.ScheduleId) &&
                (o.Status == "Completed") &&
                o.Tickets.Any(t => t.CheckInStatus == "Checked")
            );
        }
        
        public async Task<bool> HasBookingAsync(List<int> scheduleIds)
        {
            if (scheduleIds == null || !scheduleIds.Any()) return false;

            return await _context.Orders.AnyAsync(o =>scheduleIds.Contains(o.ScheduleId));
        }

        public async Task<Order> AddAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Tickets)
                .Include(o => o.Tickets)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order?> GetByIdAndCustomerIdAsync(int id, int customerId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Tickets)
                .Include(o => o.Tickets)
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId);
        }

        public async Task<IEnumerable<Order>> GetByScheduleIdAsync(int scheduleId)
        {
            return await _context.Orders
                .Where(o => o.ScheduleId == scheduleId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Tickets)
                .Include(o => o.Tickets)
                .OrderBy(o => o.Id)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
        {
            return await _context.Orders
                .Where(o => o.CustomerId == userId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Tickets)
                .Include(o => o.Tickets)
                .OrderByDescending(o => o.OrderedAt)
                .ToListAsync();
        }

        public async Task<(List<Order> Orders, int Total)> GetByUserIdPagedAsync(int userId, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Orders
                .Where(o => o.CustomerId == userId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Tickets)
                .Include(o => o.Tickets);

            var total = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.OrderedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, total);
        }

        public async Task<List<int>> GetCustomerIdsWithMinTotalSpendAsync(
            long minAmount,
            DateTime? periodFrom,
            DateTime? periodTo)
        {
            var q = BasePaidOrdersQuery(periodFrom, periodTo);

            return await q
                .GroupBy(o => o.CustomerId)
                .Where(g => g.Sum(o => o.FinalAmount) >= minAmount)
                .Select(g => g.Key)
                .ToListAsync();
        }

        public async Task<List<int>> GetTopCustomerIdsByTotalSpendAsync(
            int top,
            DateTime? periodFrom,
            DateTime? periodTo)
        {
            if (top <= 0)
            {
                return [];
            }

            var q = BasePaidOrdersQuery(periodFrom, periodTo);

            return await q
                .GroupBy(o => o.CustomerId)
                .Select(g => new { Id = g.Key, Total = g.Sum(o => o.FinalAmount) })
                .OrderByDescending(x => x.Total)
                .Take(top)
                .Select(x => x.Id)
                .ToListAsync();
        }

        private IQueryable<Order> BasePaidOrdersQuery(DateTime? periodFrom, DateTime? periodTo)
        {
            var q = _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == "Paid" || o.Status == "Completed");

            if (periodFrom.HasValue)
            {
                q = q.Where(o => o.OrderedAt >= periodFrom.Value);
            }

            if (periodTo.HasValue)
            {
                q = q.Where(o => o.OrderedAt <= periodTo.Value);
            }

            return q;
        }

        public async Task<bool> UpdateStatusAsync(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetPendingTicketCountByScheduleAsync(int scheduleId, int? excludeOrderId = null)
        {
            var query = _context.Orders.Where(o =>
                o.ScheduleId == scheduleId && o.Status == "Pending");

            if (excludeOrderId.HasValue)
            {
                query = query.Where(o => o.Id != excludeOrderId.Value);
            }

            return await query.SumAsync(o => o.TotalQuantity);
        }

        public async Task<bool> CancelOrderWithTicketsAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Tickets)
                .Include(o => o.Tickets)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return false;

            order.Status = "Cancelled";
            foreach (var ticket in order.Tickets)
            {
                ticket.CheckInStatus = "Cancelled";
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
