using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SocialAPI.DTOs;

namespace SocialAPI.Services.Implements
{
    public class TourApiClient : ITourApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TourApiClient> _logger;
        private readonly string _secretKey;

        public TourApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<TourApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _secretKey = configuration["InternalApi:SecretKey"] ?? "";
            
            var tourApiBaseUrl = configuration["InternalApi:TourApiBaseUrl"] ?? "https://localhost:7005";
            _httpClient.BaseAddress = new Uri(tourApiBaseUrl);
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUrl)
        {
            var request = new HttpRequestMessage(method, relativeUrl);
            request.Headers.Add("X-Internal-Key", _secretKey);
            return request;
        }

        public async Task<TourScheduleMetadataDto?> GetScheduleMetadataAsync(int scheduleId, string? bearerToken)
        {
            try
            {
                var request = CreateRequest(HttpMethod.Get, $"api/internal/tourschedules/{scheduleId}/metadata");
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<TourScheduleMetadataDto>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedule metadata for schedule {ScheduleId}", scheduleId);
                return null;
            }
        }

        public async Task<TourRouteDto?> GetTourRouteAsync(int scheduleId)
        {
            try
            {
                var request = CreateRequest(HttpMethod.Get, $"api/TourSchedules/{scheduleId}/route");
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var wrapper = await response.Content.ReadFromJsonAsync<TourRouteResponseWrapper>();
                    return wrapper?.Data;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tour route for schedule {ScheduleId}", scheduleId);
                return null;
            }
        }

        public async Task<bool> VerifyManagerAsync(int scheduleId, int managerId)
        {
            try
            {
                var request = CreateRequest(HttpMethod.Get, $"api/internal/tourschedules/verify-manager?scheduleId={scheduleId}&managerId={managerId}");
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<VerifyManagerResponse>();
                    return result?.IsOwner ?? false;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying manager {ManagerId} for schedule {ScheduleId}", managerId, scheduleId);
                return false;
            }
        }

        public async Task<List<int>> GetStaffIdsByScheduleAsync(int scheduleId)
        {
            try
            {
                var request = CreateRequest(HttpMethod.Get, $"api/internal/tourschedules/{scheduleId}/staff-ids");
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<StaffIdsResponse>();
                    return result?.StaffIds ?? new List<int>();
                }
                return new List<int>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff IDs for schedule {ScheduleId}", scheduleId);
                return new List<int>();
            }
        }

        public async Task<List<int>> GetStaffScheduleIdsAsync(int staffId)
        {
            try
            {
                var request = CreateRequest(HttpMethod.Get, $"api/internal/tourschedules/staff/{staffId}/schedule-ids");
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ScheduleIdsResponse>();
                    return result?.ScheduleIds ?? new List<int>();
                }
                return new List<int>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedule IDs for staff {StaffId}", staffId);
                return new List<int>();
            }
        }

        public async Task<List<int>> GetManagerScheduleIdsAsync(int managerId)
        {
            try
            {
                var request = CreateRequest(HttpMethod.Get, $"api/internal/tourschedules/manager/{managerId}/schedule-ids");
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ScheduleIdsResponse>();
                    return result?.ScheduleIds ?? new List<int>();
                }
                return new List<int>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedule IDs for manager {ManagerId}", managerId);
                return new List<int>();
            }
        }

        private class VerifyManagerResponse
        {
            public bool IsOwner { get; set; }
        }

        private class StaffIdsResponse
        {
            public List<int> StaffIds { get; set; } = new List<int>();
        }

        private class ScheduleIdsResponse
        {
            public List<int> ScheduleIds { get; set; } = new List<int>();
        }

        private class TourRouteResponseWrapper
        {
            public bool Success { get; set; }
            public TourRouteDto? Data { get; set; }
        }
    }
}
