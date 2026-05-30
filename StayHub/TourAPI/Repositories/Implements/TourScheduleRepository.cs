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

        public async Task<IEnumerable<TourSchedule>> GetAllAsync()
        {
            return await _context.TourSchedules
                .Include(ts => ts.Tour)
                .ToListAsync();
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
