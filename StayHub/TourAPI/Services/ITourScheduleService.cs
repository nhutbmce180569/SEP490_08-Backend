using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface ITourScheduleService
    {
        Task<IEnumerable<ReadTourScheduleDTO>> GetAllSchedulesAsync();
        Task<ReadTourScheduleDTO> GetScheduleByIdAsync(int id);
        Task<ReadTourScheduleDTO> CreateScheduleAsync(CreateTourScheduleDTO dto);
        Task<ReadTourScheduleDTO> UpdateScheduleAsync(int id, UpdateTourScheduleDTO dto);
        Task DeleteScheduleAsync(int id);
        Task ReserveSeatsAsync(int id, ReserveScheduleSeatsDTO dto);
        Task ReleaseSeatsAsync(int id, ReleaseScheduleSeatsDTO dto);
    }
}
