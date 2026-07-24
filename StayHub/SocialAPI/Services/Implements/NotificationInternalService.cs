using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SocialAPI.Services.Implements
{
    public class NotificationInternalService : INotificationInternalService
    {
        private readonly HttpClient _httpClient;

        public NotificationInternalService(HttpClient httpClient)
        {
            _httpClient = httpClient;
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
            response.EnsureSuccessStatusCode();
        }
    }
}
