namespace PaymentAPI.Services.Implements
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

        public async Task<bool> MarkOrderPaidAsync(int orderId)
        {
            if (_httpClient.BaseAddress == null || orderId <= 0)
            {
                return false;
            }

            try
            {
                var response = await _httpClient.PatchAsync($"api/orders/{orderId}/mark-paid", null);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                _logger.LogWarning(
                    "Failed to mark order {OrderId} as paid. Status: {StatusCode}",
                    orderId,
                    response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling BookingAPI to mark order {OrderId} as paid", orderId);
                return false;
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId)
        {
            if (_httpClient.BaseAddress == null || orderId <= 0)
            {
                return false;
            }

            try
            {
                var response = await _httpClient.PatchAsync($"api/orders/{orderId}/cancel", null);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                _logger.LogWarning(
                    "Failed to cancel order {OrderId}. Status: {StatusCode}",
                    orderId,
                    response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling BookingAPI to cancel order {OrderId}", orderId);
                return false;
            }
        }
    }
}
