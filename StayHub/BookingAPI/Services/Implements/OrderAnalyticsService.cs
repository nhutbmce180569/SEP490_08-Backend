using BookingAPI.DTOs;
using BookingAPI.Repositories;

namespace BookingAPI.Services.Implements
{
    public class OrderAnalyticsService : IOrderAnalyticsService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IContentApiClient _contentApiClient;
        private readonly ITourApiClient _tourApiClient;

        public OrderAnalyticsService(IOrderRepository orderRepository, IContentApiClient contentApiClient, ITourApiClient tourApiClient)
        {
            _orderRepository = orderRepository;
            _contentApiClient = contentApiClient;
            _tourApiClient = tourApiClient;
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

        public async Task<BookingStatisticsResponseDTO> GetBookingStatisticsAsync(BookingStatisticsRequestDTO request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request body is required.");
            }

            ValidateDateRange(request.StartDate, request.EndDate);
            request.GroupBy = NormalizeBookingStatisticsGroupBy(request.GroupBy);

            var result = await _orderRepository.GetBookingStatisticsAsync(request);

            if (result.SalesByTicketType != null && result.SalesByTicketType.Count > 0)
            {
                var activeTicketTypes = await _contentApiClient.GetActiveTicketTypesAsync();
                var ticketTypeDict = activeTicketTypes.ToDictionary(t => t.Id, t => t.Name);

                foreach (var item in result.SalesByTicketType)
                {
                    if (ticketTypeDict.TryGetValue(item.TicketTypeId, out var typeName))
                    {
                        item.TicketTypeName = typeName;
                    }
                    else
                    {
                        var typeDto = await _contentApiClient.GetTicketTypeByIdAsync(item.TicketTypeId);
                        item.TicketTypeName = typeDto?.Name ?? $"Loại #{item.TicketTypeId}";
                    }
                }
            }

            if (result.SalesByEvent != null && result.SalesByEvent.Count > 0)
            {
                foreach (var item in result.SalesByEvent)
                {
                    var schedule = await _tourApiClient.GetScheduleByIdAsync(item.ScheduleId);
                    if (schedule != null)
                    {
                        var tour = await _tourApiClient.GetTourByIdAsync(schedule.TourId);
                        item.TourName = tour?.Name ?? $"Tour #{schedule.TourId}";
                    }
                    else
                    {
                        item.TourName = $"Lịch trình #{item.ScheduleId}";
                    }
                }
            }

            return result;
        }

        public async Task<List<TopCustomerOrderDTO>> GetTopCustomersAsync(
            int top,
            DateTime? from,
            DateTime? to)
        {
            ValidateDateRange(from, to);
            if (top <= 0 || top > 1000)
            {
                throw new ArgumentException("Top must be between 1 and 1000.");
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
            var resolvedFrom = from ?? resolvedTo.AddYears(-10);
            ValidateDateRange(resolvedFrom, resolvedTo);
            return (resolvedFrom, resolvedTo);
        }

        private static void ValidateDateRange(DateTime? from, DateTime? to)
        {
            if (from.HasValue && to.HasValue && from.Value > to.Value)
            {
                throw new ArgumentException("Start date must be before or equal to end date.");
            }

            if (from.HasValue && to.HasValue && (to.Value - from.Value).TotalDays > 3650)
            {
                throw new ArgumentException("Date range cannot exceed 10 years.");
            }
        }

        private static string NormalizeGranularity(string granularity)
        {
            var normalized = (granularity ?? "day").Trim().ToLowerInvariant();
            return normalized is "day" or "week" or "month" ? normalized : "day";
        }

        private static string NormalizeBookingStatisticsGroupBy(string groupBy)
        {
            var normalized = (groupBy ?? "Day").Trim();
            return normalized.ToLowerInvariant() switch
            {
                "day" => "Day",
                "month" => "Month",
                "year" => "Year",
                _ => throw new ArgumentException("GroupBy must be Day, Month, or Year.")
            };
        }
    }
}
