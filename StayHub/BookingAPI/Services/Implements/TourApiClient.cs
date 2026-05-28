using System;
using System.Net.Http;
using System.Net.Http.Json;
using BookingAPI.DTOs;

namespace BookingAPI.Services.Implements
{
    public class TourApiClient : ITourApiClient
    {
        private readonly HttpClient _httpClient;

        public TourApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ReadOrderTourDTO?> GetTourByIdAsync(int tourId)
        {
            if (tourId <= 0 || _httpClient.BaseAddress == null)
            {
                return null;
            }

            try
            {
                return await _httpClient.GetFromJsonAsync<ReadOrderTourDTO>($"api/tours/public/{tourId}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<ReadOrderScheduleDTO?> GetScheduleByIdAsync(int scheduleId)
        {
            if (scheduleId <= 0 || _httpClient.BaseAddress == null)
            {
                return null;
            }

            try
            {
                return await _httpClient.GetFromJsonAsync<ReadOrderScheduleDTO>($"api/tourschedules/{scheduleId}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> ReserveScheduleSeatsAsync(int scheduleId, int quantity)
        {
            if (scheduleId <= 0 || quantity <= 0 || _httpClient.BaseAddress == null)
            {
                return false;
            }

            try
            {
                var response = await _httpClient.PatchAsJsonAsync(
                    $"api/tourschedules/{scheduleId}/reserve-seats",
                    new { quantity });

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ReleaseScheduleSeatsAsync(int scheduleId, int quantity)
        {
            if (scheduleId <= 0 || quantity <= 0 || _httpClient.BaseAddress == null)
            {
                return false;
            }

            try
            {
                var response = await _httpClient.PatchAsJsonAsync(
                    $"api/tourschedules/{scheduleId}/release-seats",
                    new { quantity });

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
