using System.Net.Http.Json;

namespace VoucherAPI.Services.Implements
{
    public class NotificationInternalService : INotificationInternalService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public NotificationInternalService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            _configuration = configuration;
        }

        public async Task NotifyUserAsync(int userId, string title, string content)
        {
            var gatewayUrl = (_configuration["Gateway:BaseUrl"] ?? "https://localhost:7010").TrimEnd('/');
            var requestUrl = $"{gatewayUrl}/api/notifications/internal/send";

            var payload = new
            {
                UserId = userId,
                Title = title,
                Content = content
            };

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = JsonContent.Create(payload)
            };

            var internalKey = _configuration["InternalService:Key"];
            if (!string.IsNullOrEmpty(internalKey))
            {
                request.Headers.Add("X-Service-Key", internalKey);
            }

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to send notification to SystemAPI.");
            }
        }
    }
}
