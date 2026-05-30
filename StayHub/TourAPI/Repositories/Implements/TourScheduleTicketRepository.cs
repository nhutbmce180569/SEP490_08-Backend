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

        public async Task SetActiveAsync(TourScheduleTicket entity, bool isActive)
        {
            entity.IsActive = isActive;
            _context.TourScheduleTickets.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByScheduleAndTicketTypeAsync(int scheduleId, int ticketTypeId, int? exceptId = null)
        {
            return await _context.TourScheduleTickets.AnyAsync(x =>
                x.ScheduleId == scheduleId &&
                x.TicketTypeId == ticketTypeId &&
                (exceptId == null || x.Id != exceptId));
        }

        public async Task<bool> ReserveAsync(int id, int quantity)
        {
            if (quantity <= 0)
            {
                return false;
            }

            var affected = await _context.TourScheduleTickets
                .Where(x =>
                    x.Id == id &&
                    (x.IsActive ?? true) &&
                    x.AvailableQuantity >= quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.SoldQuantity, x => (x.SoldQuantity ?? 0) + quantity)
                    .SetProperty(x => x.AvailableQuantity, x => x.AvailableQuantity - quantity));

            return affected == 1;
        }

        public async Task<bool> ReleaseAsync(int id, int quantity)
        {
            if (quantity <= 0)
            {
                return false;
            }

            var affected = await _context.TourScheduleTickets
                .Where(x =>
                    x.Id == id &&
                    (x.SoldQuantity ?? 0) >= quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.SoldQuantity, x => (x.SoldQuantity ?? 0) - quantity)
                    .SetProperty(x => x.AvailableQuantity, x => x.AvailableQuantity + quantity));

            return affected == 1;
        }
    }
}
