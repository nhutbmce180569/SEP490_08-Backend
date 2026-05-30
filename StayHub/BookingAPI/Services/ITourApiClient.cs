using BookingAPI.DTOs;

namespace BookingAPI.Services
{
    public interface ITourApiClient
    {
        Task<ReadOrderTourDTO?> GetTourByIdAsync(int tourId);
        Task<ReadOrderScheduleDTO?> GetScheduleByIdAsync(int scheduleId);
        Task<ReadOrderScheduleTicketDTO?> GetScheduleTicketByIdAsync(int tourScheduleTicketId);
        Task<bool> ReserveScheduleTicketAsync(int tourScheduleTicketId, int quantity);
        Task<bool> ReleaseScheduleTicketAsync(int tourScheduleTicketId, int quantity);
    }
}
