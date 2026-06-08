using BookingAPI.DTOs;

namespace BookingAPI.Services
{
    public interface IContentApiClient
    {
        Task<List<TicketTypeResponseDTO>> GetActiveTicketTypesAsync();
        Task<TicketTypeResponseDTO?> GetTicketTypeByIdAsync(int ticketTypeId);
    }
}
