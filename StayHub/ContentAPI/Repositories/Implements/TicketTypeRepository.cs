using ContentAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ContentAPI.Repositories.Implements
{
    public class TicketTypeRepository : ITicketTypeRepository
    {
        private readonly StayHubContentDbContext _context;

        public TicketTypeRepository(StayHubContentDbContext context)
        {
            _context = context;
        }

        public async Task<List<TicketType>> GetAllAsync()
        {
            return await _context.TicketTypes
                .AsNoTracking()
                .OrderByDescending(t => t.Id)
                .ToListAsync();
        }

        public async Task<List<TicketType>> GetActiveAsync()
        {
            return await _context.TicketTypes
                .AsNoTracking()
                .Where(t => t.IsActive == true)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<TicketType?> GetById(int id)
        {
            return await _context.TicketTypes.FirstOrDefaultAsync(t => t.Id == id);
        }
        public async Task<TicketType?> GetByName(string name)
        {
            return await _context.TicketTypes.FirstOrDefaultAsync(t => t.Name.ToLower().Equals(name.ToLower()));
        }
        public async Task<TicketType> Add(TicketType ticketType)
        {
            _context.TicketTypes.Add(ticketType);
            await _context.SaveChangesAsync();
            return ticketType;
        }

        public async Task Update(TicketType ticketType)
        {
            _context.TicketTypes.Update(ticketType);
            await _context.SaveChangesAsync();
        }
    }
}
