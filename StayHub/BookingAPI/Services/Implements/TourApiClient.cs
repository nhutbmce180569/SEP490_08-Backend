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

        public async Task<IReadOnlyCollection<int>> GetManagedScheduleIdsAsync(int operatorId, int? tourId = null)
        {
            if (operatorId <= 0)
            {
                throw new UnauthorizedAccessException("Operator identity could not be verified.");
            }

            if (_httpClient.BaseAddress == null)
            {
                throw new InvalidOperationException("Tour API base address is not configured.");
            }

            string url = "api/tourschedules/my/ids";
            if (tourId.HasValue && tourId.Value > 0)
            {
                url += $"?tourId={tourId.Value}";
            }

            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<int>>() ?? new List<int>();
        }

        public async Task<ReadOrderScheduleTicketDTO?> GetScheduleTicketByIdAsync(int tourScheduleTicketId)
        {
            if (tourScheduleTicketId <= 0 || _httpClient.BaseAddress == null)
            {
                return null;
            }

            try
            {
                return await _httpClient.GetFromJsonAsync<ReadOrderScheduleTicketDTO>(
                    $"api/tourscheduletickets/{tourScheduleTicketId}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> ReserveScheduleTicketAsync(int tourScheduleTicketId, int quantity)
        {
            if (tourScheduleTicketId <= 0 || quantity <= 0 || _httpClient.BaseAddress == null)
            {
                return false;
            }

            try
            {
                var response = await _httpClient.PatchAsJsonAsync(
                    $"api/tourscheduletickets/{tourScheduleTicketId}/reserve",
                    new { quantity });

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ReleaseScheduleTicketAsync(int tourScheduleTicketId, int quantity)
        {
            if (tourScheduleTicketId <= 0 || quantity <= 0 || _httpClient.BaseAddress == null)
            {
                return false;
            }

            try
            {
                var response = await _httpClient.PatchAsJsonAsync(
                    $"api/tourscheduletickets/{tourScheduleTicketId}/release",
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
