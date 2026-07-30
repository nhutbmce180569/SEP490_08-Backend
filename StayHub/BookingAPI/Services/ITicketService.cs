using BookingAPI.DTOs;

namespace BookingAPI.Services
{
    public interface ITicketService
    {
        Task<CheckInResultDTO?> CheckInTicketAsync(CheckInRequestDTO request);
        Task<ReadTicketDTO?> GetTicketByQrCodeAsync(string qrCode);
        Task<List<ReadTicketDTO>> GetTicketsByUserIdAsync(int userId);
        Task<List<ReadTicketDTO>> GetTicketsByScheduleIdAsync(int scheduleId, string? attendeeName = null, string? checkInStatus = null);
    }
}
