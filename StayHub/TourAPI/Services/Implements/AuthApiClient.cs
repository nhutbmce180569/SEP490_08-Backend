using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TourAPI.DTOs;
using TourAPI.Services;

namespace TourAPI.Services.Implements
{
    public class AuthApiClient : IAuthApiClient
    {
        private readonly HttpClient _httpClient;

        public AuthApiClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            var baseUrl = configuration.GetValue<string>("AuthApi:BaseUrl") ?? "https://localhost:7001";
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Add("X-Service-Key", configuration.GetValue<string>("InternalService:Key"));
        }

        public async Task<List<AuthCustomerSummary>> GetUsersByRoleAsync(string role)
        {
            var response = await _httpClient.GetAsync($"api/internal/users/by-role?role={role}");
            if (!response.IsSuccessStatusCode)
            {
                return new List<AuthCustomerSummary>();
            }

            var payload = await response.Content.ReadFromJsonAsync<List<AuthCustomerSummary>>();
            return payload ?? new List<AuthCustomerSummary>();
        }
    }
}
