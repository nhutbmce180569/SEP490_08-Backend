using System.Net.Http.Json;
using BookingAPI.DTOs;

namespace BookingAPI.Services.Implements;

public class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;

    public AuthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<BatchUserProfileDTO>> GetUsersBatchAsync(List<int> userIds)
    {
        if (userIds == null || !userIds.Any() || _httpClient.BaseAddress == null)
        {
            return new List<BatchUserProfileDTO>();
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/users/batch", userIds);
            if (!response.IsSuccessStatusCode)
            {
                return new List<BatchUserProfileDTO>();
            }

            var payload = await response.Content.ReadFromJsonAsync<BatchResponse<List<BatchUserProfileDTO>>>();
            return payload?.Data ?? new List<BatchUserProfileDTO>();
        }
        catch
        {
            return new List<BatchUserProfileDTO>();
        }
    }

    private sealed class BatchResponse<T>
    {
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
}
