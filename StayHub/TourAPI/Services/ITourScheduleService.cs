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
        Task<List<ItineraryLocationDto>> GetItinerariesByScheduleIdAsync(int scheduleId);
        Task<IEnumerable<ReadTourScheduleDTO>> GetSchedulesByIdsAsync(IEnumerable<int> scheduleIds);
    }
}
