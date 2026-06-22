using System.Net.Http.Json;
using VoucherAPI.DTOs;

namespace VoucherAPI.Services.Implements;

public class UserValidationService : IUserValidationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public UserValidationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<(bool Exists, string? FullName, string? Email, string? Status)> ValidateUserAsync(int userId)
    {
        var gatewayUrl = _configuration["Gateway:BaseUrl"] ?? "https://localhost:7010";
        var response = await _httpClient.GetAsync($"{gatewayUrl}/api/users/{userId}");

        if (!response.IsSuccessStatusCode)
        {
            return (false, null, null, null);
        }

        var wrapper = await response.Content.ReadFromJsonAsync<UserApiResponse>();
        return (
            wrapper?.Data != null,
            wrapper?.Data?.FullName,
            wrapper?.Data?.Email,
            wrapper?.Data?.Status);
    }

    public async Task<List<ReadUserApiDTO>> GetCustomersByBirthdayMonthAsync(int month)
    {
        var gatewayUrl = _configuration["Gateway:BaseUrl"] ?? "https://localhost:7010";
        
        var request = new HttpRequestMessage(HttpMethod.Get, $"{gatewayUrl}/api/internal/users/birthdays?month={month}");
        var internalKey = _configuration["InternalService:Key"];
        if (!string.IsNullOrEmpty(internalKey))
        {
            request.Headers.Add("X-Service-Key", internalKey);
        }

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return new List<ReadUserApiDTO>();
        }

        var users = await response.Content.ReadFromJsonAsync<List<ReadUserApiDTO>>();
        return users ?? new List<ReadUserApiDTO>();
    }
}
