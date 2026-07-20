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

        public async Task<bool> CheckCompletedBookingAsync(int customerId, List<int> scheduleIds)
        {
            if (customerId <= 0 || scheduleIds == null || !scheduleIds.Any() || _httpClient.BaseAddress == null)
                return false;

            try
            {
                var requestBody = new { CustomerId = customerId, ScheduleIds = scheduleIds };
                var response = await _httpClient.PostAsJsonAsync("api/orders/check-completed-booking", requestBody);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<bool>();
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check completed booking for customer {CustomerId}.", customerId);
                return false;
            }
        }

        public async Task<List<int>> GetEligibleScheduleIdsByUserAsync(int userId)
        {
            if (userId <= 0 || _httpClient.BaseAddress == null)
                return new List<int>();

            try
            {
                var response = await _httpClient.GetFromJsonAsync<EligibleSchedulesInternalResponse>(
                    $"api/orders/users/{userId}/eligible-schedule-ids");

                return response?.Data ?? new List<int>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get eligible schedule ids for user {UserId}.", userId);
                return new List<int>();
            }
        }

        private class EligibleSchedulesInternalResponse
        {
            public List<int> Data { get; set; } = new();
        }

        // DTO nội bộ
        private class CustomerIdsResponse
        {
            public List<int> Data { get; set; } = new();
        }
    }
}
