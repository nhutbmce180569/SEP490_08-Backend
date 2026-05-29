using BookingAPI.DTOs;

namespace BookingAPI.Services
{
    public interface ITourApiClient
    {
        Task<ReadOrderTourDTO?> GetTourByIdAsync(int tourId);
        Task<ReadOrderScheduleDTO?> GetScheduleByIdAsync(int scheduleId);
        Task<bool> ReserveScheduleSeatsAsync(int scheduleId, int quantity);
        Task<bool> ReleaseScheduleSeatsAsync(int scheduleId, int quantity);
    }
}
