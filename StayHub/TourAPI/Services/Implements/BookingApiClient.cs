using System.Net.Http.Json;

namespace TourAPI.Services.Implements
{
    public class BookingApiClient : IBookingApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BookingApiClient> _logger;

        public BookingApiClient(HttpClient httpClient, ILogger<BookingApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> HasOrdersForScheduleAsync(int scheduleId)
        {
            if (scheduleId <= 0 || _httpClient.BaseAddress == null)
                return false;

            try
            {
                var response = await _httpClient.GetFromJsonAsync<ScheduleOrderCheckDto>(
                    $"api/orders/schedules/{scheduleId}/has-orders");

                return response?.HasOrders ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check orders for schedule {ScheduleId}. Defaulting to false.", scheduleId);
                return false;
            }
        }

        // DTO nội bộ để parse response từ BookingAPI
        private class ScheduleOrderCheckDto
        {
            public bool HasOrders { get; set; }
            public int OrderCount { get; set; }
        }
    }
}