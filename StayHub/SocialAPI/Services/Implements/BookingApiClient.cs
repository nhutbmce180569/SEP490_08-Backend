namespace SocialAPI.Services.Implements
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
        // BookingApiClient.cs — implement
        public async Task<List<int>> GetCustomerIdsByScheduleAsync(int scheduleId)
        {
            if (scheduleId <= 0 || _httpClient.BaseAddress == null)
                return new List<int>();

            try
            {
                var response = await _httpClient.GetFromJsonAsync<CustomerIdsResponse>(
                    $"api/orders/schedules/{scheduleId}/customer-ids");

                return response?.Data ?? new List<int>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get customer ids for schedule {ScheduleId}.", scheduleId);
                return new List<int>();
            }
        }

        // DTO nội bộ
        private class CustomerIdsResponse
        {
            public List<int> Data { get; set; } = new();
        }
    }
}
