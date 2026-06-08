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
            return await _context.Tickets
                .Include(t => t.OrderDetail)
                .FirstOrDefaultAsync(t => t.QrCode == qrCode);
        }

        public async Task<Ticket?> GetReadOnlyByQrCodeAsync(string qrCode)
        {
            return await _context.Tickets
                .AsNoTracking()
                .Include(t => t.OrderDetail)
                .FirstOrDefaultAsync(t => t.QrCode == qrCode);
        }

        public async Task<List<Ticket>> GetByUserIdAsync(int userId)
        {
            return await _context.Tickets
                .Include(t => t.OrderDetail)
                    .ThenInclude(od => od.Order)
                .AsNoTracking()
                .Where(t => t.UserId == userId || t.OrderDetail.Order.CustomerId == userId)
                .ToListAsync();
        }

        public async Task<List<Ticket>> GetByScheduleIdAsync(int scheduleId)
        {
            return await _context.Tickets
                .Include(t => t.OrderDetail)
                    .ThenInclude(od => od.Order)
                .AsNoTracking()
                .Where(t => t.OrderDetail.Order.ScheduleId == scheduleId)
                .ToListAsync();
        }

        public async Task UpdateAsync(Ticket ticket)
        {
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();
        }
    }
}
