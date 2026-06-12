namespace SystemAPI.Services.Implements
{
    public class AuthApiClient : IAuthApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string? _internalKey;

        public AuthApiClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _internalKey = configuration["InternalApi:SecretKey"]; // ✅ lấy key 1 lần trong constructor
        }

        public async Task<string?> GetFcmTokenAsync(int userId)
        {
            if (userId <= 0 || _httpClient.BaseAddress == null)
                return null;

            try
            {
                // ✅ Thêm secret key vào header
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/users/internal/{userId}/fcm-token"
                );
                request.Headers.Add("X-Internal-Key", _internalKey);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;

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
            if (userId <= 0 || _httpClient.BaseAddress == null) return;

            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Delete,
                    $"api/users/internal/{userId}/fcm-token"
                );
                request.Headers.Add("X-Internal-Key", _internalKey);

                await _httpClient.SendAsync(request);
            }
            catch
            {
                // ignore
            }
        }

        private sealed class FcmTokenResponse
        {
            public string? Token { get; set; }
        }
    }
}