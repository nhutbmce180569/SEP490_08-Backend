using BookingAPI.Models;

namespace BookingAPI.Repositories
{
    public interface ITicketRepository
    {
        Task<Ticket?> GetByQrCodeAsync(string qrCode);
        Task<Ticket?> GetReadOnlyByQrCodeAsync(string qrCode);
        Task<List<Ticket>> GetByUserIdAsync(int userId);
        Task<List<Ticket>> GetByScheduleIdAsync(int scheduleId);
        Task UpdateAsync(Ticket ticket);
    }
}
