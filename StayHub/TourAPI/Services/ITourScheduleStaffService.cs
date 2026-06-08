using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface ITourScheduleStaffService
    {
        Task AssignStaffToScheduleAsync(AssignStaffRequestDto dto);

        Task RemoveStaffFromScheduleAsync(int scheduleId, int staffId);

        Task<List<ScheduleStaffDetailDto>> GetStaffByScheduleIdAsync(int scheduleId);

        Task<PaginationDTO<AssignedTourScheduleDto>> GetAssignedSchedulesAsync(
    int staffId, int page, int pageSize, bool upcomingOnly, string? tourName = null);
    }
}
