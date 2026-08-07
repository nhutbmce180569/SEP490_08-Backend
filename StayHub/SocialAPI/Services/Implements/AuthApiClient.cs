using System.Net.Http.Json;
using SocialAPI.DTOs;

namespace SocialAPI.Services.Implements
{
    public class AuthApiClient : IAuthApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string? _internalKey;

        public AuthApiClient(IConfiguration configuration)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(configuration["AuthApi:BaseUrl"] ?? "https://localhost:7001/")
            };
            _internalKey = configuration["InternalApi:SecretKey"];
        }

        public async Task<Dictionary<int, UserProfileShortDto>> GetUserProfilesAsync(
            IEnumerable<int> userIds)
        {
            var ids = userIds?.Distinct().Where(id => id > 0).ToList();
            if (ids == null || !ids.Any() || _httpClient.BaseAddress == null)
                return new Dictionary<int, UserProfileShortDto>();

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/users/batch", ids);
                Console.WriteLine($"[AuthApiClient] Batch Request sent for {ids.Count} users. Status: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[AuthApiClient] Error response: {err}");
                    return new Dictionary<int, UserProfileShortDto>();
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[AuthApiClient] JSON Response: {jsonResponse}");

                var payload = System.Text.Json.JsonSerializer.Deserialize<BatchResponse<List<UserProfileShortDto>>>(
                    jsonResponse, 
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                var dict = payload?.Data?.ToDictionary(u => u.Id, u => u) ?? new Dictionary<int, UserProfileShortDto>();
                Console.WriteLine($"[AuthApiClient] Parsed {dict.Count} user profiles.");
                return dict;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthApiClient] Error fetching user profiles: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[AuthApiClient] Inner Exception: {ex.InnerException.Message}");
                return new Dictionary<int, UserProfileShortDto>();
            }
        }

        public async Task<UserProfileShortDto?> GetUserProfileAsync(int userId)
        {
            if (userId <= 0 || _httpClient.BaseAddress == null)
                return null;

            try
            {
                var response = await _httpClient
                    .GetFromJsonAsync<BatchResponse<UserProfileShortDto>>(
                        $"api/users/{userId}/profile");
                return response?.Data;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> GetFcmTokenAsync(int userId)
        {
            if (userId <= 0 || _httpClient.BaseAddress == null)
                return null;

            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/users/internal/{userId}/fcm-token");
                request.Headers.Add("X-Internal-Key", _internalKey);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                var result = await response.Content.ReadFromJsonAsync<FcmTokenResponse>();
                return result?.Token;
            }
            catch
            {
                return null;
            }
        }

        public async Task ClearFcmTokenAsync(int userId)
        {
            if (userId <= 0 || _httpClient.BaseAddress == null)
                return;

            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Delete,
                    $"api/users/internal/{userId}/fcm-token");
                request.Headers.Add("X-Internal-Key", _internalKey);
                await _httpClient.SendAsync(request);
            }
            catch
            {
                // ignore
            }
        }

        public async Task<List<int>> GetUsersByRoleAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName) || _httpClient.BaseAddress == null)
                return new List<int>();

            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/internal/users/by-role?role={roleName}");
                
                request.Headers.Add("X-Service-Key", "stayhub-internal-2025-xK9mP");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<int>();

                var users = await response.Content.ReadFromJsonAsync<List<UserProfileShortDto>>();
                return users?.Select(u => u.Id).ToList() ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        private sealed class BatchResponse<T>
        {
            public string? Message { get; set; }
            public T? Data { get; set; }
        }

        private sealed class FcmTokenResponse
        {
            public string? Token { get; set; }
        }
    }
}