using BookingAPI.DTOs;
using BookingAPI.Repositories;

namespace BookingAPI.Services.Implements
{
    public class PlatformAnalyticsService : IPlatformAnalyticsService
    {
        private readonly IOrderRepository _orderRepository;

        public PlatformAnalyticsService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<PlatformOperationsStatsDTO> GetOperationsStatsAsync(DateTime? from, DateTime? to)
        {
            ValidateDateRange(from, to);
            return await _orderRepository.GetPlatformOperationsStatsAsync(from, to);
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
    }
}
