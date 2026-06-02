using TourAPI.Models;

namespace TourAPI.Repositories
{
    public interface ITourScheduleStaffRepository
    {
        Task<bool> IsStaffAssignedAsync(int scheduleId, int staffId);

        Task AssignStaffAsync(TourScheduleStaff entity);

        Task<TourScheduleStaff?> GetAssignedStaffAsync(int scheduleId, int staffId);

        Task RemoveStaffAsync(TourScheduleStaff entity);

        Task<List<TourScheduleStaff>> GetStaffByScheduleIdAsync(int scheduleId);
    }
}
