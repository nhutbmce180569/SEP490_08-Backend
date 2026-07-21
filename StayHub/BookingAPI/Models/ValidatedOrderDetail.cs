using BookingAPI.DTOs;

namespace BookingAPI.Models
{
    internal sealed class ValidatedOrderDetail
    {
        public int TourScheduleTicketId { get; init; }
        public int TicketTypeId { get; init; }
        public int Quantity { get; init; }
        public long UnitPrice { get; init; }
        public long TotalPrice { get; init; }
        public long PromotionDiscountValue { get; init; }
        public List<CreateTicketDTO> Tickets { get; init; } = new List<CreateTicketDTO>();
    }
}
