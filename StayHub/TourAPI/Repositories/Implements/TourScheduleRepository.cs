using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TourAPI.Models;

namespace TourAPI.Repositories.Implements
{
    public class TourScheduleRepository : ITourScheduleRepository
    {
        private readonly StayHubCatalogDbContext _context;

        public TourScheduleRepository(StayHubCatalogDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TourSchedule>> GetAllAsync(int page, int pageSize)
        {
            return await _context.TourSchedules
                .Include(ts => ts.Tour)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAllAsync()
        {
            return await _context.TourSchedules.CountAsync();
        }

        public async Task<TourSchedule?> GetByIdAsync(int id)
        {
            return await _context.TourSchedules
                .Include(x => x.Tour)
                .Include(x => x.TourScheduleItineraries)
                .Include(x => x.TourScheduleStaffs)
                .Include(x => x.TourScheduleTickets)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<TourSchedule>> GetByIdsAsync(List<int> ids)
        {
            return await _context.TourSchedules
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();
        }

        public async Task<List<TourSchedule>> GetByTourIdAsync(int tourId)
        {
            return await _context.TourSchedules
                .Where(x => x.TourId == tourId)
                .ToListAsync();
        }

        public async Task<TourSchedule?> GetScheduleWithItineraryAsync(int scheduleId)
        {
            return await _context.TourSchedules
                .Include(x => x.Tour)
                .Include(x => x.TourScheduleItineraries)
                .FirstOrDefaultAsync(x => x.Id == scheduleId);
        }

        public async Task<IEnumerable<TourSchedule>> GetByCreatedByAsync(int userId, int page, int pageSize)
        {
            return await _context.TourSchedules
                .Include(ts => ts.Tour)
                .Where(ts => ts.Tour != null && ts.Tour.CreatedBy == userId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountByCreatedByAsync(int userId)
        {
            return await _context.TourSchedules
                .Include(ts => ts.Tour)
                .Where(ts => ts.Tour != null && ts.Tour.CreatedBy == userId)
                .CountAsync();
        }

        public async Task<IEnumerable<TourSchedule>> SearchByTourNameAsync(string tourName, int page, int pageSize)
        {
            return await _context.TourSchedules
                .Include(ts => ts.Tour)
                .Where(ts => ts.Tour != null &&
                       ts.Tour.Name.ToLower().Contains(tourName.ToLower()))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountByTourNameAsync(string tourName)
        {
            return await _context.TourSchedules
                .Include(ts => ts.Tour)
                .Where(ts => ts.Tour != null &&
                       ts.Tour.Name.ToLower().Contains(tourName.ToLower()))
                .CountAsync();
        }

        public async Task AddAsync(TourSchedule tourSchedule)
        {
            await _context.TourSchedules.AddAsync(tourSchedule);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TourSchedule tourSchedule)
        {
            _context.TourSchedules.Update(tourSchedule);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(TourSchedule tourSchedule)
        {
            _context.TourSchedules.Remove(tourSchedule);
            await _context.SaveChangesAsync();
        }
    }
}
