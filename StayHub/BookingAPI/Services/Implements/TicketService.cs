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

        public async Task<ReadTicketDTO?> CheckInTicketAsync(UpdateTicketDTO request)
        {
            if (string.IsNullOrEmpty(request.QrCode)) return null;

            var ticket = await _ticketRepository.GetByQrCodeAsync(request.QrCode);
            if (ticket == null) return null;

            // Cập nhật trạng thái check-in
            ticket.CheckInStatus = "Checked";

            await _ticketRepository.UpdateAsync(ticket);
            return _mapper.Map<ReadTicketDTO>(ticket);
        }

        public async Task<ReadTicketDTO?> GetTicketByQrCodeAsync(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode)) return null;

            var ticket = await _ticketRepository.GetReadOnlyByQrCodeAsync(qrCode);
            return ticket == null ? null : _mapper.Map<ReadTicketDTO>(ticket);
        }
    }
}
