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
        try
        {
            var gatewayUrl = _configuration["Gateway:BaseUrl"] ?? "https://localhost:7010";
            var urls = new[]
            {
                $"{gatewayUrl.TrimEnd('/')}/api/internal/users/{userId}",
                $"https://localhost:7010/api/internal/users/{userId}",
                $"https://localhost:7001/api/internal/users/{userId}"
            };

            var internalKey = _configuration["InternalService:Key"] ?? _configuration["InternalApi:SecretKey"] ?? "StayHub_Internal_Service_Key_2026";

            foreach (var url in urls)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    if (!string.IsNullOrEmpty(internalKey))
                    {
                        request.Headers.Add("X-Service-Key", internalKey);
                    }

                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var options = new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        var wrapper = await response.Content.ReadFromJsonAsync<UserApiResponse>(options);
                        if (wrapper?.Data != null)
                        {
                            return (true, wrapper.Data.FullName, wrapper.Data.Email, wrapper.Data.Status);
                        }
                    }
                }
                catch
                {
                    // Fallback to next URL
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserValidationService] Error validating user {userId}: {ex.Message}");
        }

        return (false, null, null, null);
    }

    public async Task<List<ReadUserApiDTO>> GetCustomersByBirthdayMonthAsync(int month)
    {
        var gatewayUrl = _configuration["Gateway:BaseUrl"] ?? "https://localhost:7010";
        
        var request = new HttpRequestMessage(HttpMethod.Get, $"{gatewayUrl.TrimEnd('/')}/api/internal/users/birthdays?month={month}");
        var internalKey = _configuration["InternalService:Key"] ?? "StayHub_Internal_Service_Key_2026";
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

    public async Task<List<ReadUserApiDTO>> GetUsersBatchAsync(List<int> userIds)
    {
        if (userIds == null || userIds.Count == 0) return new List<ReadUserApiDTO>();

        try
        {
            var gatewayUrl = _configuration["Gateway:BaseUrl"] ?? "https://localhost:7010";
            var urls = new[]
            {
                $"{gatewayUrl.TrimEnd('/')}/api/internal/users/batch",
                $"https://localhost:7010/api/internal/users/batch",
                $"https://localhost:7001/api/internal/users/batch"
            };

            var internalKey = _configuration["InternalService:Key"] ?? _configuration["InternalApi:SecretKey"] ?? "StayHub_Internal_Service_Key_2026";

            foreach (var url in urls)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Content = JsonContent.Create(userIds);
                    if (!string.IsNullOrEmpty(internalKey))
                    {
                        request.Headers.Add("X-Service-Key", internalKey);
                    }

                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var options = new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        var users = await response.Content.ReadFromJsonAsync<List<ReadUserApiDTO>>(options);
                        return users ?? new List<ReadUserApiDTO>();
                    }
                }
                catch
                {
                    // Fallback to next URL
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserValidationService] Error getting users batch: {ex.Message}");
        }

        return new List<ReadUserApiDTO>();
    }

    public async Task<List<int>> GetAllActiveCustomerIdsAsync()
    {
        var gatewayUrl = _configuration["Gateway:BaseUrl"] ?? "https://localhost:7010";
        var request = new HttpRequestMessage(HttpMethod.Get, $"{gatewayUrl.TrimEnd('/')}/api/internal/users/active-customers/ids");
        
        var internalKey = _configuration["InternalService:Key"] ?? _configuration["InternalApi:SecretKey"] ?? "StayHub_Internal_Service_Key_2026";
        if (!string.IsNullOrEmpty(internalKey))
        {
            request.Headers.Add("X-Service-Key", internalKey);
        }

        try
        {
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var ids = await response.Content.ReadFromJsonAsync<List<int>>();
                return ids ?? new List<int>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserValidationService] Error getting active customer ids: {ex.Message}");
        }

        return new List<int>();
    }
}
