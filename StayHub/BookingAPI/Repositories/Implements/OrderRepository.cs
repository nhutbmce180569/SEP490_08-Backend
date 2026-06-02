using BookingAPI.DTOs;
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

        private static readonly string[] PaidStatuses = ["Paid", "Completed"];

        public async Task<OrderAnalyticsOverviewDTO> GetOrderOverviewAsync(DateTime? from, DateTime? to)
        {
            var periodOrders = await FilterOrdersQuery(from, to).ToListAsync();
            var paidPeriod = periodOrders.Where(o => PaidStatuses.Contains(o.Status ?? "")).ToList();

            var cancellations = await _context.CancellationRequests.AsNoTracking().ToListAsync();
            var periodCancellations = cancellations.Where(c =>
                (!from.HasValue || c.RequestedAt >= from.Value) &&
                (!to.HasValue || c.RequestedAt <= to.Value)).ToList();

            var uniqueCustomers = paidPeriod.Select(o => o.CustomerId).Distinct().Count();
            var repeatCustomers = paidPeriod
                .GroupBy(o => o.CustomerId)
                .Count(g => g.Count() >= 2);

            var totalOrders = periodOrders.Count;
            var cancelled = periodOrders.Count(o => o.Status == "Cancelled");

            return new OrderAnalyticsOverviewDTO
            {
                TotalOrders = totalOrders,
                PaidOrders = periodOrders.Count(o => o.Status == "Paid"),
                CompletedOrders = periodOrders.Count(o => o.Status == "Completed"),
                PendingOrders = periodOrders.Count(o => o.Status == "Pending"),
                CancelledOrders = cancelled,
                TotalRevenue = paidPeriod.Sum(o => o.FinalAmount),
                AverageOrderValue = paidPeriod.Count > 0
                    ? paidPeriod.Sum(o => o.FinalAmount) / paidPeriod.Count
                    : 0,
                UniqueCustomersWithOrders = uniqueCustomers,
                RepeatCustomers = repeatCustomers,
                RepeatCustomerRate = uniqueCustomers > 0
                    ? Math.Round(repeatCustomers * 100m / uniqueCustomers, 2)
                    : 0,
                TotalTicketsSold = paidPeriod.Sum(o => o.TotalQuantity),
                TotalDiscountApplied = paidPeriod.Sum(o => o.DiscountValue ?? 0),
                OrdersWithVoucher = paidPeriod.Count(o => !string.IsNullOrWhiteSpace(o.VoucherCode)),
                PendingCancellationRequests = periodCancellations.Count(c => c.Status == "Pending"),
                ApprovedCancellationRequests = periodCancellations.Count(c =>
                    c.Status is "Approved" or "Refunded"),
                TotalRefundAmount = periodCancellations
                    .Where(c => c.Status is "Approved" or "Refunded")
                    .Sum(c => c.RefundAmount),
                CancellationRate = totalOrders > 0
                    ? Math.Round(cancelled * 100m / totalOrders, 2)
                    : 0
            };
        }

        public async Task<CustomerOrderSegmentDTO> GetCustomerOrderSegmentsAsync(
            DateTime? from,
            DateTime? to,
            int totalCustomers)
        {
            var allPaid = await FilterOrdersQuery(null, null)
                .Where(o => PaidStatuses.Contains(o.Status ?? ""))
                .ToListAsync();

            var periodPaid = await FilterOrdersQuery(from, to)
                .Where(o => PaidStatuses.Contains(o.Status ?? ""))
                .ToListAsync();

            var customersWithOrders = allPaid.Select(o => o.CustomerId).Distinct().ToHashSet();
            var orderCounts = allPaid.GroupBy(o => o.CustomerId)
                .ToDictionary(g => g.Key, g => g.Count());

            var firstOrderDates = allPaid
                .GroupBy(o => o.CustomerId)
                .ToDictionary(g => g.Key, g => g.Min(o => o.OrderedAt));

            var periodFrom = from ?? DateTime.UtcNow.AddDays(-30);
            var periodTo = to ?? DateTime.UtcNow;
            var atRiskThreshold = DateTime.UtcNow.AddDays(-90);

            var newBuyers = firstOrderDates.Count(kvp =>
                kvp.Value.HasValue &&
                kvp.Value.Value >= periodFrom &&
                kvp.Value.Value <= periodTo);

            var atRisk = allPaid
                .GroupBy(o => o.CustomerId)
                .Count(g =>
                {
                    var lastOrder = g.Max(o => o.OrderedAt);
                    return lastOrder.HasValue && lastOrder.Value < atRiskThreshold;
                });

            var spendByCustomer = allPaid
                .GroupBy(o => o.CustomerId)
                .Select(g => g.Sum(o => o.FinalAmount))
                .OrderByDescending(x => x)
                .ToList();

            var highValueThreshold = spendByCustomer.Count > 0
                ? spendByCustomer[(int)Math.Max(0, Math.Ceiling(spendByCustomer.Count * 0.2) - 1)]
                : 0;

            var highValue = allPaid
                .GroupBy(o => o.CustomerId)
                .Count(g => g.Sum(o => o.FinalAmount) >= highValueThreshold && highValueThreshold > 0);

            var oneTime = orderCounts.Count(kvp => kvp.Value == 1);
            var repeat = orderCounts.Count(kvp => kvp.Value >= 2);
            var neverPurchased = Math.Max(0, totalCustomers - customersWithOrders.Count);

            return new CustomerOrderSegmentDTO
            {
                NeverPurchased = neverPurchased,
                OneTimeBuyers = oneTime,
                RepeatBuyers = repeat,
                NewBuyersInPeriod = newBuyers,
                AtRiskCustomers = atRisk,
                HighValueCustomers = highValue,
                BuyerConversionRate = totalCustomers > 0
                    ? Math.Round(customersWithOrders.Count * 100m / totalCustomers, 2)
                    : 0
            };
        }

        public async Task<List<OrderTrendPointDTO>> GetOrderTrendsAsync(
            DateTime from,
            DateTime to,
            string granularity)
        {
            var orders = await FilterOrdersQuery(from, to)
                .Where(o => PaidStatuses.Contains(o.Status ?? ""))
                .ToListAsync();

            return orders
                .Where(o => o.OrderedAt.HasValue)
                .GroupBy(o => FormatPeriod(o.OrderedAt!.Value, granularity))
                .Select(g => new OrderTrendPointDTO
                {
                    Period = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.FinalAmount),
                    UniqueCustomers = g.Select(o => o.CustomerId).Distinct().Count()
                })
                .OrderBy(x => x.Period)
                .ToList();
        }

        public async Task<List<TopCustomerOrderDTO>> GetTopCustomersAsync(
            int top,
            DateTime? from,
            DateTime? to)
        {
            if (top <= 0) return [];

            var orders = await FilterOrdersQuery(from, to)
                .Where(o => PaidStatuses.Contains(o.Status ?? ""))
                .ToListAsync();

            return orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new TopCustomerOrderDTO
                {
                    CustomerId = g.Key,
                    TotalSpend = g.Sum(o => o.FinalAmount),
                    OrderCount = g.Count(),
                    LastOrderAt = g.Max(o => o.OrderedAt),
                    TotalTickets = g.Sum(o => o.TotalQuantity)
                })
                .OrderByDescending(x => x.TotalSpend)
                .Take(top)
                .ToList();
        }

        public async Task<List<CustomerOrderMetricsDTO>> GetCustomerOrderMetricsAsync(
            List<int> customerIds,
            DateTime? from,
            DateTime? to)
        {
            if (customerIds == null || customerIds.Count == 0) return [];

            var orders = await FilterOrdersQuery(from, to)
                .Where(o => customerIds.Contains(o.CustomerId))
                .ToListAsync();

            return customerIds.Select(id => BuildCustomerMetrics(id, orders)).ToList();
        }

        public async Task<CustomerOrderMetricsDTO?> GetCustomerOrderMetricsByIdAsync(
            int customerId,
            DateTime? from,
            DateTime? to)
        {
            var orders = await FilterOrdersQuery(from, to)
                .Where(o => o.CustomerId == customerId)
                .ToListAsync();

            if (!orders.Any()) return null;

            return BuildCustomerMetrics(customerId, orders);
        }

        private static CustomerOrderMetricsDTO BuildCustomerMetrics(int customerId, List<Order> orders)
        {
            var customerOrders = orders.Where(o => o.CustomerId == customerId).ToList();
            var paid = customerOrders.Where(o => PaidStatuses.Contains(o.Status ?? "")).ToList();

            return new CustomerOrderMetricsDTO
            {
                CustomerId = customerId,
                TotalOrders = customerOrders.Count,
                PaidOrders = paid.Count,
                TotalSpend = paid.Sum(o => o.FinalAmount),
                AverageOrderValue = paid.Count > 0 ? paid.Sum(o => o.FinalAmount) / paid.Count : 0,
                FirstOrderAt = customerOrders.Min(o => o.OrderedAt),
                LastOrderAt = customerOrders.Max(o => o.OrderedAt),
                TotalTickets = paid.Sum(o => o.TotalQuantity),
                CancelledOrders = customerOrders.Count(o => o.Status == "Cancelled"),
                PendingOrders = customerOrders.Count(o => o.Status == "Pending")
            };
        }

        private IQueryable<Order> FilterOrdersQuery(DateTime? from, DateTime? to)
        {
            var q = _context.Orders.AsNoTracking();
            if (from.HasValue) q = q.Where(o => o.OrderedAt >= from.Value);
            if (to.HasValue) q = q.Where(o => o.OrderedAt <= to.Value);
            return q;
        }

        private static string FormatPeriod(DateTime date, string granularity)
        {
            return granularity switch
            {
                "week" => $"{date.Year}-W{System.Globalization.ISOWeek.GetWeekOfYear(date):D2}",
                "month" => date.ToString("yyyy-MM"),
                _ => date.ToString("yyyy-MM-dd")
            };
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

        public async Task<List<int>> GetEligibleScheduleIdsByUserIdAsync(int userId)
        {
            return await _context.Orders
                .Where(o => o.CustomerId == userId && (o.Status == "Paid" || o.Status == "Completed"))
                .Select(o => o.ScheduleId)
                .Distinct() 
                .ToListAsync();
        }
    }
    
}
