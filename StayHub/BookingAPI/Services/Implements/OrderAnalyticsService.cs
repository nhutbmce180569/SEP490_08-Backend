using BookingAPI.DTOs;
using BookingAPI.Repositories;

namespace BookingAPI.Services.Implements
{
    public class OrderAnalyticsService : IOrderAnalyticsService
    {
        private readonly IOrderRepository _orderRepository;

        public OrderAnalyticsService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<OrderAnalyticsOverviewDTO> GetOverviewAsync(DateTime? from, DateTime? to)
        {
            ValidateDateRange(from, to);
            return await _orderRepository.GetOrderOverviewAsync(from, to);
        }

        public async Task<CustomerOrderSegmentDTO> GetSegmentsAsync(
            DateTime? from,
            DateTime? to,
            int totalCustomers)
        {
            ValidateDateRange(from, to);
            if (totalCustomers < 0)
            {
                throw new ArgumentException("Total customers cannot be negative.");
            }

            return await _orderRepository.GetCustomerOrderSegmentsAsync(from, to, totalCustomers);
        }

        public async Task<List<OrderTrendPointDTO>> GetTrendsAsync(
            DateTime? from,
            DateTime? to,
            string granularity)
        {
            var range = ResolveDateRange(from, to);
            granularity = NormalizeGranularity(granularity);
            return await _orderRepository.GetOrderTrendsAsync(range.From, range.To, granularity);
        }

        public async Task<List<TopCustomerOrderDTO>> GetTopCustomersAsync(
            int top,
            DateTime? from,
            DateTime? to)
        {
            ValidateDateRange(from, to);
            if (top <= 0 || top > 50)
            {
                throw new ArgumentException("Top must be between 1 and 50.");
            }

            return await _orderRepository.GetTopCustomersAsync(top, from, to);
        }

        public async Task<List<CustomerOrderMetricsDTO>> GetCustomerMetricsAsync(
            List<int> customerIds,
            DateTime? from,
            DateTime? to)
        {
            ValidateDateRange(from, to);
            return await _orderRepository.GetCustomerOrderMetricsAsync(customerIds, from, to);
        }

        public async Task<CustomerOrderMetricsDTO?> GetCustomerMetricsByIdAsync(
            int customerId,
            DateTime? from,
            DateTime? to)
        {
            ValidateDateRange(from, to);
            if (customerId <= 0)
            {
                throw new ArgumentException("Invalid customer ID.");
            }

            return await _orderRepository.GetCustomerOrderMetricsByIdAsync(customerId, from, to);
        }

        public (DateTime From, DateTime To) ResolveDateRange(DateTime? from, DateTime? to)
        {
            var resolvedTo = to ?? DateTime.UtcNow;
            var resolvedFrom = from ?? resolvedTo.AddDays(-30);
            ValidateDateRange(resolvedFrom, resolvedTo);
            return (resolvedFrom, resolvedTo);
        }

        private static void ValidateDateRange(DateTime? from, DateTime? to)
        {
            if (from.HasValue && to.HasValue && from.Value > to.Value)
            {
                throw new ArgumentException("Start date must be before or equal to end date.");
            }

            if (from.HasValue && to.HasValue && (to.Value - from.Value).TotalDays > 366)
            {
                throw new ArgumentException("Date range cannot exceed 366 days.");
            }
        }

        private static string NormalizeGranularity(string granularity)
        {
            var normalized = (granularity ?? "day").Trim().ToLowerInvariant();
            return normalized is "day" or "week" or "month" ? normalized : "day";
        }
    }
}
