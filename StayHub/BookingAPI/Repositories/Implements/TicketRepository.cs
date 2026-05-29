using BookingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingAPI.Repositories.Implements
{
    public class TicketRepository : ITicketRepository
    {
        private readonly StayHubBookingDbContext _context;

        public TicketRepository(StayHubBookingDbContext context)
        {
            _context = context;
        }

        public async Task<Ticket?> GetByQrCodeAsync(string qrCode)
        {
            return await _context.Tickets.FirstOrDefaultAsync(t => t.QrCode == qrCode);
        }

        public async Task<Ticket?> GetReadOnlyByQrCodeAsync(string qrCode)
        {
            return await _context.Tickets
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.QrCode == qrCode);
        }

        public async Task UpdateAsync(Ticket ticket)
        {
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();
        }
    }
}
