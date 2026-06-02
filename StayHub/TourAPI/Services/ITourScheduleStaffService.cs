using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface ITourScheduleStaffService
    {
        Task AssignStaffToScheduleAsync(AssignStaffRequestDto dto);

        Task RemoveStaffFromScheduleAsync(int scheduleId, int staffId);

        Task<List<ScheduleStaffDetailDto>> GetStaffByScheduleIdAsync(int scheduleId);
    }
}
