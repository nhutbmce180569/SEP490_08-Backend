using AuthAPI.DTOs;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AuthAPI.Services.Implements
{
    public class SocialAuthService : ISocialAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public SocialAuthService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<SocialAuthResultDTO?> ValidateGoogleTokenAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new List<string> { _configuration["Google:ClientId"]! }
                };
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                return new SocialAuthResultDTO
                {
                    Email = payload.Email,
                    Name = payload.Name,
                    AvatarUrl = payload.Picture
                };
            }
            catch (InvalidJwtException)
            {
                return null;
            }
        }

        public async Task<SocialAuthResultDTO?> ValidateFacebookTokenAsync(string accessToken)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"https://graph.facebook.com/me?fields=id,email,name,picture.type(large)&access_token={accessToken}");

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var fbData = JObject.Parse(content);

            string? email = fbData["email"]?.ToString();
            if (string.IsNullOrEmpty(email))
                return null;

            string? name = fbData["name"]?.ToString();
            string? avatarUrl = fbData["picture"]?["data"]?["url"]?.ToString();

            return new SocialAuthResultDTO
            {
                Email = email,
                Name = name ?? "Facebook User",
                AvatarUrl = avatarUrl
            };
        }
    }
}
