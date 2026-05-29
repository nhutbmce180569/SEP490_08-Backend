using BookingAPI.Models;

namespace BookingAPI.Repositories
{
    public interface ITicketRepository
    {
        Task<Ticket?> GetByQrCodeAsync(string qrCode);
        Task<Ticket?> GetReadOnlyByQrCodeAsync(string qrCode);
        Task UpdateAsync(Ticket ticket);
    }
}
