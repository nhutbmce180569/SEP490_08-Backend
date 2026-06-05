using System.Net.Http.Json;
using VoucherAPI.DTOs;

namespace VoucherAPI.Services.Implements;

public class BookingAnalyticsClient : IBookingAnalyticsClient
{
    private readonly HttpClient _httpClient;

    public BookingAnalyticsClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var gatewayUrl = configuration["Gateway:BaseUrl"] ?? "https://localhost:7010";
        _httpClient.BaseAddress = new Uri(gatewayUrl.TrimEnd('/') + "/");
    }

    public async Task<List<BookingTopCustomer>> GetTopCustomersAsync(
        int top,
        DateTime? from,
        DateTime? to)
    {
        var response = await _httpClient.GetAsync(
            $"api/internal/analytics/orders/top-customers?top={top}&{BuildDateQuery(from, to)}");

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<BookingTopCustomersResponse>();
        return payload?.Data ?? [];
    }

    private static string BuildDateQuery(DateTime? from, DateTime? to)
    {
        var parts = new List<string>();
        if (from.HasValue) parts.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) parts.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        return string.Join("&", parts);
    }
}
