using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TourAPI.Services;

namespace TourAPI.Services.Implements
{
    public class NotificationInternalService : INotificationInternalService
    {
        private readonly HttpClient _httpClient;

        public NotificationInternalService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            var baseUrl = configuration.GetValue<string>("SystemApi:BaseUrl") ?? "https://localhost:7009";
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public async Task NotifyUserAsync(int userId, string title, string content)
        {
            var payload = new
            {
                UserId = userId,
                Title = title,
                Content = content
            };

            var response = await _httpClient.PostAsJsonAsync("api/notifications/internal/send", payload);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to send notification to SystemAPI.");
            }
        }
    }
}
