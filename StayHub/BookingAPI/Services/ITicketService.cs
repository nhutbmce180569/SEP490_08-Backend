using BookingAPI.DTOs;

namespace BookingAPI.Services
{
    public interface ITicketService
    {
        Task<ReadTicketDTO?> CheckInTicketAsync(UpdateTicketDTO request);
        Task<ReadTicketDTO?> GetTicketByQrCodeAsync(string qrCode);
    }
}
