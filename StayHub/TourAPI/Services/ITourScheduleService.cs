using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface ITourScheduleService
    {
        Task<PaginationDTO<ReadTourScheduleDTO>> GetAllSchedulesAsync(int page, int pageSize);
        Task<ReadTourScheduleDTO> GetScheduleByIdAsync(int id);
        Task<ReadTourScheduleDTO> CreateScheduleAsync(CreateTourScheduleDTO dto);
        Task<ReadTourScheduleDTO> UpdateScheduleAsync(int id, UpdateTourScheduleDTO dto);
        Task DeleteScheduleAsync(int id);
        Task<List<ItineraryLocationDto>> GetItinerariesByScheduleIdAsync(int scheduleId);
        Task<IEnumerable<ReadTourScheduleDTO>> GetSchedulesByIdsAsync(IEnumerable<int> scheduleIds);
        Task<PaginationDTO<ReadTourScheduleDTO>> SearchSchedulesByTourNameAsync(string tourName, int page, int pageSize);
        Task<PaginationDTO<ReadTourScheduleDTO>> GetSchedulesByCreatedByAsync(int userId, int page, int pageSize, int? tourId = null, DateTime? startDate = null, DateTime? endDate = null, string? search = null);
        Task<List<int>> GetScheduleIdsByCreatedByAsync(int userId, int? tourId = null);
        Task<TourRouteDto> GetTourRouteAsync(int scheduleId);
    }
}
