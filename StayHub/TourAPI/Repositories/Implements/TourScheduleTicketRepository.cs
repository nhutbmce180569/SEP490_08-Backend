using Microsoft.EntityFrameworkCore;
using TourAPI.Models;

namespace TourAPI.Repositories.Implements
{
    public class TourScheduleTicketRepository : ITourScheduleTicketRepository
    {
        private readonly StayHubCatalogDbContext _context;

        public TourScheduleTicketRepository(StayHubCatalogDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TourScheduleTicket>> GetAllAsync()
        {
            return await _context.TourScheduleTickets
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<IEnumerable<TourScheduleTicket>> GetByScheduleIdAsync(int scheduleId)
        {
            return await _context.TourScheduleTickets
                .Where(x => x.ScheduleId == scheduleId)
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<TourScheduleTicket?> GetByIdAsync(int id)
        {
            return await _context.TourScheduleTickets.FindAsync(id);
        }

        public async Task AddAsync(TourScheduleTicket entity)
        {
            await _context.TourScheduleTickets.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TourScheduleTicket entity)
        {
            _context.TourScheduleTickets.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(TourScheduleTicket entity)
        {
            _context.TourScheduleTickets.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByScheduleAndTicketTypeAsync(int scheduleId, int ticketTypeId, int? exceptId = null)
        {
            return await _context.TourScheduleTickets.AnyAsync(x =>
                x.ScheduleId == scheduleId &&
                x.TicketTypeId == ticketTypeId &&
                (exceptId == null || x.Id != exceptId));
        }
    }
}
