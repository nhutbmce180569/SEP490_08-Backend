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
        private readonly ITourApiClient _tourApiClient;

        public TicketService(ITicketRepository ticketRepository, IMapper mapper, IContentApiClient contentApiClient, ITourApiClient tourApiClient)
        {
            _ticketRepository = ticketRepository;
            _mapper = mapper;
            _contentApiClient = contentApiClient;
            _tourApiClient = tourApiClient;
        }

        public async Task<CheckInResultDTO> CheckInTicketAsync(CheckInRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.QrCode))
                throw new ArgumentException("QR code cannot be empty.");

            // 1. Lấy ticket kèm OrderDetail -> Order
            var ticket = await _ticketRepository.GetByQrCodeAsync(request.QrCode);
            if (ticket == null)
                throw new KeyNotFoundException("QR code is invalid or does not exist.");

            // 2. Kiểm tra trạng thái đã check-in chưa
            if (ticket.CheckInStatus == "CheckedIn")
                throw new InvalidOperationException("This ticket has already been checked in. Please do not scan again.");

            if (ticket.CheckInStatus == "Cancelled")
                throw new InvalidOperationException("This ticket has been cancelled. Check-in cannot be performed.");

            // 3. Lấy scheduleId từ Order. 
            if (ticket.OrderDetail?.Order == null)
                throw new InvalidOperationException("Ticket data error: corresponding order not found.");

            if (ticket.OrderDetail.Order.Status != "Paid")
                throw new InvalidOperationException(
                    $"This ticket's order has not been paid yet. Current status: {ticket.OrderDetail.Order.Status}.");

            var scheduleId = ticket.OrderDetail.Order.ScheduleId;

            var schedule = await _tourApiClient.GetScheduleByIdAsync(scheduleId);
            if (schedule == null)
                throw new KeyNotFoundException($"Schedule information for ID {scheduleId} not found.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var departureDate = DateOnly.FromDateTime(schedule.DepartureDate);

            if (departureDate != today)
            {
                throw new InvalidOperationException(
                    $"This ticket belongs to a schedule departing on {departureDate:dd/MM/yyyy}. " +
                    $"Today is {today:dd/MM/yyyy}, check-in is not allowed.");
            }

            // 5. Gọi ContentAPI lấy tên loại vé
            var ticketType = await _contentApiClient.GetTicketTypeByIdAsync(ticket.TicketTypeId);
            var ticketTypeName = ticketType?.Name ?? "Unknown";

            // 6. Cập nhật vé
            ticket.CheckInStatus = "CheckedIn";

            await _ticketRepository.UpdateAsync(ticket);

            // 7. Trả kết quả
            return new CheckInResultDTO
            {
                TicketId = ticket.Id,
                AttendeeName = ticket.AttendeeName,
                TicketTypeName = ticketTypeName,
                CheckInStatus = ticket.CheckInStatus,
                ScheduleId = scheduleId,
                DepartureDate = schedule.DepartureDate
            };
        }
        public async Task<ReadTicketDTO?> GetTicketByQrCodeAsync(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode)) return null;

            var ticket = await _ticketRepository.GetReadOnlyByQrCodeAsync(qrCode);
            return ticket == null ? null : _mapper.Map<ReadTicketDTO>(ticket);
        }

        public async Task<List<ReadTicketDTO>> GetTicketsByScheduleIdAsync(int scheduleId, string? attendeeName = null, string? checkInStatus = null)
        {
            var tickets = await _ticketRepository.GetByScheduleIdAsync(
                scheduleId, attendeeName, checkInStatus);

            var ticketDtos = _mapper.Map<List<ReadTicketDTO>>(tickets);

            if (!ticketDtos.Any()) return ticketDtos;

            var activeTicketTypes = await _contentApiClient.GetActiveTicketTypesAsync();
            var ticketTypeDict = activeTicketTypes.ToDictionary(t => t.Id, t => t.Name);

            foreach (var dto in ticketDtos)
            {
                if (ticketTypeDict.TryGetValue(dto.TicketTypeId, out var typeName))
                    dto.TicketTypeName = typeName;
                else
                    dto.TicketTypeName = "Unknown Type";
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
