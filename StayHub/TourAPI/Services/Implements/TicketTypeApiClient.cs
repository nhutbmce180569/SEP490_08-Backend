using System.Net.Http.Json;
using TourAPI.DTOs;

namespace TourAPI.Services.Implements
{
    public class TicketTypeApiClient : ITicketTypeApiClient
    {
        private readonly HttpClient _httpClient;

        public TicketTypeApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ReadTicketTypeDTO?> GetTicketTypeByIdAsync(int ticketTypeId)
        {
            if (ticketTypeId <= 0 || _httpClient.BaseAddress == null)
            {
                return null;
            }

            try
            {
                return await _httpClient.GetFromJsonAsync<ReadTicketTypeDTO>($"api/tickettypes/{ticketTypeId}");
            }
            catch
            {
                return null;
            }
        }
    }
}
