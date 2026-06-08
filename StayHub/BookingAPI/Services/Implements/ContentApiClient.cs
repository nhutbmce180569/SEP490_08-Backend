using BookingAPI.DTOs;

namespace BookingAPI.Services.Implements
{
    public class ContentApiClient : IContentApiClient
    {
        private readonly HttpClient _httpClient;

        public ContentApiClient(HttpClient httpClient)
        {
            // Nhớ cấu hình BaseAddress của ContentAPI trong Program.cs nhé
            _httpClient = httpClient;
        }

        public async Task<List<TicketTypeResponseDTO>> GetActiveTicketTypesAsync()
        {
            try
            {
                // Gọi API lấy danh sách loại vé đang active từ ContentAPI
                var response = await _httpClient.GetFromJsonAsync<List<TicketTypeResponseDTO>>("/api/TicketTypes/active");
                return response ?? new List<TicketTypeResponseDTO>();
            }
            catch
            {
                // Nếu ContentAPI chết, trả về list rỗng để BookingAPI không bị sập theo
                return new List<TicketTypeResponseDTO>();
            }
        }

        public async Task<TicketTypeResponseDTO?> GetTicketTypeByIdAsync(int ticketTypeId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<TicketTypeResponseDTO>(
                    $"/api/TicketTypes/{ticketTypeId}");
            }
            catch
            {
                return null;
            }
        }
    }
}
