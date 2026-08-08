using System.Net.Http.Json;
using VoucherAPI.DTOs;

namespace VoucherAPI.Services.Implements;

public class TourValidationService : ITourValidationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public TourValidationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<(bool Exists, string? Name, string? Status, int CreatedBy)> ValidateTourAsync(int tourId)
    {
        var gatewayUrl = (_configuration["Gateway:BaseUrl"] ?? "https://localhost:7010").TrimEnd('/');
        var response = await _httpClient.GetAsync($"{gatewayUrl}/api/tours/public/{tourId}");

        if (!response.IsSuccessStatusCode)
        {
            return (false, null, null, 0);
        }

        var tour = await response.Content.ReadFromJsonAsync<TourApiResponse>();
        return (tour != null, tour?.Name, tour?.Status, tour?.CreatedBy ?? 0);
    }
}
