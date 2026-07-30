using System.Net.Http.Json;

namespace PaymentAPI.Services.Implements
{
    public class BookingApiClient : IBookingApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BookingApiClient> _logger;
        private readonly IConfiguration _configuration;

        public BookingApiClient(
            HttpClient httpClient,
            ILogger<BookingApiClient> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public Task<bool> MarkOrderPaidAsync(int orderId, string? customerEmail)
        {
            return ApplyPaymentResultAsync(orderId, true, customerEmail);
        }

        public Task<bool> CancelOrderAsync(int orderId)
        {
            return ApplyPaymentResultAsync(orderId, false, null);
        }

        private async Task<bool> ApplyPaymentResultAsync(
            int orderId,
            bool isSuccess,
            string? customerEmail)
        {
            if (_httpClient.BaseAddress == null || orderId <= 0)
            {
                return false;
            }

            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using var request = new HttpRequestMessage(
                        HttpMethod.Post,
                        $"api/orders/{orderId}/internal-payment-result")
                    {
                        Content = JsonContent.Create(new
                        {
                            isSuccess,
                            customerEmail
                        })
                    };
                    request.Headers.TryAddWithoutValidation(
                        "X-StayHub-Service-Key",
                        _configuration["InternalService:Key"]);

                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }

                    _logger.LogWarning(
                        "Failed to apply payment result for order {OrderId}. Attempt: {Attempt}. Status: {StatusCode}",
                        orderId,
                        attempt + 1,
                        response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error applying payment result for order {OrderId}. Attempt: {Attempt}",
                        orderId,
                        attempt + 1);
                }

                if (attempt < 2)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * (attempt + 1)));
                }
            }

            return false;
        }
    }
}
