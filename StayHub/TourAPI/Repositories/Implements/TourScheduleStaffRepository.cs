using Microsoft.EntityFrameworkCore;
using TourAPI.Models;

namespace TourAPI.Repositories.Implements
{
    public class TourScheduleStaffRepository : ITourScheduleStaffRepository
    {
        private readonly StayHubCatalogDbContext _context;

        public TourScheduleStaffRepository(StayHubCatalogDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsStaffAssignedAsync(int scheduleId, int staffId)
        {
            return await _context.TourScheduleStaffs
                .AnyAsync(x => x.ScheduleId == scheduleId && x.StaffId == staffId);
        }

        public async Task AssignStaffAsync(TourScheduleStaff entity)
        {
            await _context.TourScheduleStaffs.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<TourScheduleStaff?> GetAssignedStaffAsync(int scheduleId, int staffId)
        {
            return await _context.TourScheduleStaffs
                .FirstOrDefaultAsync(x => x.ScheduleId == scheduleId && x.StaffId == staffId);
        }

        public async Task RemoveStaffAsync(TourScheduleStaff entity)
        {
            _context.TourScheduleStaffs.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TourScheduleStaff>> GetStaffByScheduleIdAsync(int scheduleId)
        {
            return await _context.TourScheduleStaffs
                .Where(x => x.ScheduleId == scheduleId)
                .ToListAsync();
        }
    }
}
