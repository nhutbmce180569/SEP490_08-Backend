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

        public async Task<(List<TourScheduleStaff> Items, int Total)> GetAssignedSchedulesAsync(
     int staffId, int page, int pageSize, bool upcomingOnly, string? tourName = null)
        {
            var now = DateTime.UtcNow;

            var query = _context.TourScheduleStaffs
                .Where(x => x.StaffId == staffId)
                .Include(x => x.Schedule)
                    .ThenInclude(s => s.Tour)
                .AsQueryable();

            if (upcomingOnly)
                query = query.Where(x => x.Schedule.DepartureDate >= now);

            // ✅ Filter theo tên tour
            if (!string.IsNullOrWhiteSpace(tourName))
                query = query.Where(x => x.Schedule.Tour != null &&
                                         x.Schedule.Tour.Name.Contains(tourName.Trim()));

            query = upcomingOnly
                ? query.OrderBy(x => x.Schedule.DepartureDate)
                : query.OrderByDescending(x => x.Schedule.DepartureDate);

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }
    }
}
