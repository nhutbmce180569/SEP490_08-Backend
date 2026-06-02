using BookingAPI.DTOs;

namespace BookingAPI.Services
{
    public interface ITicketService
    {
        Task<ReadTicketDTO?> CheckInTicketAsync(CheckInRequestDTO request);
        Task<ReadTicketDTO?> GetTicketByQrCodeAsync(string qrCode);
        Task<List<ReadTicketDTO>> GetTicketsByUserIdAsync(int userId);
        Task<List<ReadTicketDTO>> GetTicketsByScheduleIdAsync(int scheduleId);
    }
}
