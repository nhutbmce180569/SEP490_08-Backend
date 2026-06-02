using System.Net.Http.Json;
using TourAPI.DTOs;

namespace TourAPI.Services.Implements
{
    public class PlatformAnalyticsClients : IPlatformAnalyticsClients
    {
        private readonly HttpClient _httpClient;

        public PlatformAnalyticsClients(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PlatformUserStatsData?> GetUserStatsAsync(
            DateTime? from,
            DateTime? to,
            string granularity)
        {
            var response = await _httpClient.GetAsync(
                $"api/internal/analytics/platform/users?{BuildDateQuery(from, to)}&granularity={granularity}");

            if (!response.IsSuccessStatusCode) return null;

            var payload = await response.Content.ReadFromJsonAsync<PlatformUserStatsResponse>();
            return payload?.Data;
        }

        public async Task<PlatformOperationsStatsData?> GetOperationsStatsAsync(DateTime? from, DateTime? to)
        {
            var response = await _httpClient.GetAsync(
                $"api/internal/analytics/platform/operations?{BuildDateQuery(from, to)}");

            if (!response.IsSuccessStatusCode) return null;

            var payload = await response.Content.ReadFromJsonAsync<PlatformOperationsStatsResponse>();
            return payload?.Data;
        }

        public async Task<PlatformVoucherStatsData?> GetVoucherStatsAsync()
        {
            var response = await _httpClient.GetAsync("api/internal/analytics/platform/vouchers");
            if (!response.IsSuccessStatusCode) return null;

            var payload = await response.Content.ReadFromJsonAsync<PlatformVoucherStatsResponse>();
            return payload?.Data;
        }

        public async Task<PlatformSocialStatsData?> GetSocialStatsAsync()
        {
            var response = await _httpClient.GetAsync("api/internal/analytics/platform/social");
            if (!response.IsSuccessStatusCode) return null;

            var payload = await response.Content.ReadFromJsonAsync<PlatformSocialStatsResponse>();
            return payload?.Data;
        }

        private static string BuildDateQuery(DateTime? from, DateTime? to)
        {
            var parts = new List<string>();
            if (from.HasValue) parts.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
            if (to.HasValue) parts.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
            return string.Join("&", parts);
        }
    }
}
