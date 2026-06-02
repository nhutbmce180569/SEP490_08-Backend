using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface ITicketTypeApiClient
    {
        Task<ReadTicketTypeDTO?> GetTicketTypeByIdAsync(int ticketTypeId);
    }
}
