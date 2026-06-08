using AutoMapper;
using BookingAPI.DTOs;
using BookingAPI.Repositories;

namespace BookingAPI.Services.Implements
{
    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IMapper _mapper;
        private readonly IContentApiClient _contentApiClient;

        public TicketService(ITicketRepository ticketRepository, IMapper mapper, IContentApiClient contentApiClient)
        {
            _ticketRepository = ticketRepository;
            _mapper = mapper;
            _contentApiClient = contentApiClient;
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

            return _mapper.Map<ReadTicketDTO>(ticket);
        }

        public async Task<ReadTicketDTO?> GetTicketByQrCodeAsync(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode)) return null;

            var ticket = await _ticketRepository.GetReadOnlyByQrCodeAsync(qrCode);
            return ticket == null ? null : _mapper.Map<ReadTicketDTO>(ticket);
        }

        public async Task<List<ReadTicketDTO>> GetTicketsByScheduleIdAsync(int scheduleId)
        {
            var tickets = await _ticketRepository.GetByScheduleIdAsync(scheduleId);
            var ticketDtos = _mapper.Map<List<ReadTicketDTO>>(tickets);

            if (!ticketDtos.Any()) return ticketDtos;

            var activeTicketTypes = await _contentApiClient.GetActiveTicketTypesAsync();

            var ticketTypeDict = activeTicketTypes.ToDictionary(t => t.Id, t => t.Name);

            foreach (var dto in ticketDtos)
            {
                if (ticketTypeDict.TryGetValue(dto.TicketTypeId, out var typeName))
                {
                    dto.TicketTypeName = typeName;
                }
                else
                {
                    dto.TicketTypeName = "Unknown Type"; 
                }
            }

            return ticketDtos;
        }

        public async Task<List<ReadTicketDTO>> GetTicketsByUserIdAsync(int userId)
        {
            var tickets = await _ticketRepository.GetByUserIdAsync(userId);
            var ticketDtos = _mapper.Map<List<ReadTicketDTO>>(tickets);

            if (!ticketDtos.Any()) return ticketDtos;

            var activeTicketTypes = await _contentApiClient.GetActiveTicketTypesAsync();
            var ticketTypeDict = activeTicketTypes.ToDictionary(t => t.Id, t => t.Name);

            foreach (var dto in ticketDtos)
            {
                if (ticketTypeDict.TryGetValue(dto.TicketTypeId, out var typeName))
                {
                    dto.TicketTypeName = typeName;
                }
            }

            return ticketDtos;
        }
    }
}
