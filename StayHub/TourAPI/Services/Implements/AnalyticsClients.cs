using System.Net.Http.Json;
using TourAPI.DTOs;

namespace TourAPI.Services.Implements
{
    public class AuthAnalyticsClient : IAuthAnalyticsClient
    {
        private readonly HttpClient _httpClient;

        public AuthAnalyticsClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AuthDemographicsData?> GetDemographicsAsync(
            DateTime? from,
            DateTime? to,
            string granularity)
        {
            var query = BuildDateQuery(from, to) + $"&granularity={granularity}";
            var response = await _httpClient.GetAsync($"api/internal/analytics/customers/demographics?{query}");
            if (!response.IsSuccessStatusCode) return null;

            var payload = await response.Content.ReadFromJsonAsync<AuthDemographicsResponse>();
            return payload?.Data;
        }

        public async Task<AuthCustomerListData?> GetCustomerListAsync(string? search, int page, int pageSize)
        {
            var searchPart = string.IsNullOrWhiteSpace(search)
                ? string.Empty
                : $"&search={Uri.EscapeDataString(search)}";

            var response = await _httpClient.GetAsync(
                $"api/internal/analytics/customers/list?page={page}&pageSize={pageSize}{searchPart}");

            if (!response.IsSuccessStatusCode) return null;

            var payload = await response.Content.ReadFromJsonAsync<AuthCustomerListResponse>();
            return payload?.Data;
        }

        public async Task<AuthCustomerSummary?> GetCustomerSummaryAsync(int customerId)
        {
            var response = await _httpClient.GetAsync($"api/internal/analytics/customers/{customerId}");
            if (!response.IsSuccessStatusCode) return null;

            var payload = await response.Content.ReadFromJsonAsync<AuthCustomerDetailResponse>();
            return payload?.Data;
        }

        public async Task<List<AuthCustomerSummary>> GetCustomersBatchAsync(List<int> customerIds)
        {
            if (customerIds.Count == 0) return [];

            var response = await _httpClient.PostAsJsonAsync("api/users/batch", customerIds);
            if (!response.IsSuccessStatusCode) return [];

            var payload = await response.Content.ReadFromJsonAsync<AuthCustomerBatchResponse>();
            return payload?.Data?.Select(u => new AuthCustomerSummary
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl
            }).ToList() ?? [];
        }

        private static string BuildDateQuery(DateTime? from, DateTime? to)
        {
            var parts = new List<string>();
            if (from.HasValue) parts.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
            if (to.HasValue) parts.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
            return string.Join("&", parts);
        }
    }

    public class BookingAnalyticsClient : IBookingAnalyticsClient
    {
        private readonly HttpClient _httpClient;

        public BookingAnalyticsClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<BookingOverviewData?> GetOverviewAsync(DateTime? from, DateTime? to)
        {
            var response = await _httpClient.GetAsync(
                $"api/internal/analytics/orders/overview?{BuildDateQuery(from, to)}");

            if (!response.IsSuccessStatusCode) return null;

            var payload = await response.Content.ReadFromJsonAsync<BookingOverviewResponse>();
            return payload?.Data;
        }

        public async Task<BookingSegmentsData?> GetSegmentsAsync(
            DateTime? from,
            DateTime? to,
            int totalCustomers)
        {
            var response = await _httpClient.GetAsync(
                $"api/internal/analytics/orders/segments?{BuildDateQuery(from, to)}&totalCustomers={totalCustomers}");

            if (!response.IsSuccessStatusCode) return null;

            var payload = await response.Content.ReadFromJsonAsync<BookingSegmentsResponse>();
            return payload?.Data;
        }

        public async Task<List<BookingTrendPoint>> GetTrendsAsync(
            DateTime? from,
            DateTime? to,
            string granularity)
        {
            var response = await _httpClient.GetAsync(
                $"api/internal/analytics/orders/trends?{BuildDateQuery(from, to)}&granularity={granularity}");

            if (!response.IsSuccessStatusCode) return [];

            var payload = await response.Content.ReadFromJsonAsync<BookingTrendsResponse>();
            return payload?.Data ?? [];
        }

        public async Task<List<BookingTopCustomer>> GetTopCustomersAsync(
            int top,
            DateTime? from,
            DateTime? to)
        {
            var response = await _httpClient.GetAsync(
                $"api/internal/analytics/orders/top-customers?top={top}&{BuildDateQuery(from, to)}");

            if (!response.IsSuccessStatusCode) return [];

            var payload = await response.Content.ReadFromJsonAsync<BookingTopCustomersResponse>();
            return payload?.Data ?? [];
        }

        public async Task<List<BookingCustomerMetrics>> GetCustomerMetricsAsync(
            List<int> customerIds,
            DateTime? from,
            DateTime? to)
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/internal/analytics/orders/customer-metrics?{BuildDateQuery(from, to)}",
                new { customerIds });

            if (!response.IsSuccessStatusCode) return [];

            var payload = await response.Content.ReadFromJsonAsync<BookingCustomerMetricsResponse>();
            return payload?.Data ?? [];
        }

        public async Task<BookingCustomerMetrics?> GetCustomerMetricsByIdAsync(
            int customerId,
            DateTime? from,
            DateTime? to)
        {
            var response = await _httpClient.GetAsync(
                $"api/internal/analytics/orders/customer-metrics/{customerId}?{BuildDateQuery(from, to)}");

            if (!response.IsSuccessStatusCode) return null;

            var payload = await response.Content.ReadFromJsonAsync<BookingCustomerMetricsDataResponse>();
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
