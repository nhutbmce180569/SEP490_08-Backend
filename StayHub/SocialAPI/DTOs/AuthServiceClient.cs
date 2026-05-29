using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SocialAPI.Integration
{
    public class AuthServiceClient : IAuthServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthServiceClient> _logger;

        public AuthServiceClient(HttpClient httpClient, ILogger<AuthServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<Dictionary<int, UserDto>> GetUsersBatchAsync(List<int> userIds, string? token = null)
        {
            try
            {
                if (userIds == null || !userIds.Any())
                    return new Dictionary<int, UserDto>();

                var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/batch");
                request.Content = JsonContent.Create(userIds);

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserDto>>>();
                return result?.Data?.ToDictionary(u => u.Id, u => u) ?? new Dictionary<int, UserDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching users batch from AuthAPI. Fallback to empty dictionary.");
                return new Dictionary<int, UserDto>(); // Đảm bảo SocialAPI không bị sập theo
            }
        }
    }

    public class ApiResponse<T>
    {
        public string Message { get; set; }
        public T Data { get; set; }
    }
}