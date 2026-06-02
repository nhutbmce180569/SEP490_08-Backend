using AutoMapper;
using BookingAPI.DTOs;
using BookingAPI.Repositories;

namespace BookingAPI.Services.Implements
{
    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IMapper _mapper;

        public TicketService(ITicketRepository ticketRepository, IMapper mapper)
        {
            _ticketRepository = ticketRepository;
            _mapper = mapper;
        }

        public async Task<ReadTicketDTO?> CheckInTicketAsync(CheckInRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.QrCode))
                throw new ArgumentException("Mã QR không được để trống.");

            var ticket = await _ticketRepository.GetByQrCodeAsync(request.QrCode);

            // 1. Kiểm tra vé có tồn tại không
            if (ticket == null)
                throw new KeyNotFoundException("Mã QR không hợp lệ hoặc không tồn tại trong hệ thống.");

            // 3. Kiểm tra vé đã được check-in trước đó chưa
            if (ticket.CheckInStatus == "CheckedIn")
                throw new InvalidOperationException("Vé này đã được điểm danh trước đó. Vui lòng không quét lại.");

            // 4. Cập nhật trạng thái thành công
            ticket.CheckInStatus = "CheckedIn";

            // Nếu Database của bạn có cột lưu thời gian CheckIn (VD: CheckInTime), hãy mở comment dòng dưới
            // ticket.CheckInTime = DateTime.UtcNow;

            await _ticketRepository.UpdateAsync(ticket);

            // Trả về DTO (DTO này nên chứa CustomerName, DOB... để FE hiển thị chúc mừng)
            return _mapper.Map<ReadTicketDTO>(ticket);
        }

        public async Task<ReadTicketDTO?> GetTicketByQrCodeAsync(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode)) return null;

            var ticket = await _ticketRepository.GetReadOnlyByQrCodeAsync(qrCode);
            return ticket == null ? null : _mapper.Map<ReadTicketDTO>(ticket);
        }

        public async Task<List<ReadTicketDTO>> GetTicketsByUserIdAsync(int userId)
        {
            var tickets = await _ticketRepository.GetByUserIdAsync(userId);
            return _mapper.Map<List<ReadTicketDTO>>(tickets);
        }

        public async Task<List<ReadTicketDTO>> GetTicketsByScheduleIdAsync(int scheduleId)
        {
            var tickets = await _ticketRepository.GetByScheduleIdAsync(scheduleId);
            return _mapper.Map<List<ReadTicketDTO>>(tickets);
        }
    }
}
