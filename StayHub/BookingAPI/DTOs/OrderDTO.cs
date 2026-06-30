using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookingAPI.DTOs
{
    public class InternalPaymentResultDTO
    {
        public bool IsSuccess { get; set; }
        public string? CustomerEmail { get; set; }
    }

    public class ReadOrderDTO
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }

        public int ScheduleId { get; set; }

        public int TotalQuantity { get; set; }

        public int TicketCount { get; set; }

        public long TotalAmount { get; set; }

        public long? DiscountValue { get; set; }

        public string? VoucherCode { get; set; }

        public long FinalAmount { get; set; }

        public string? Note { get; set; }

        public string? Status { get; set; }
        public DateTime? OrderedAt { get; set; }

        public string? InviteToken { get; set; }

        public ReadOrderScheduleDTO? Schedule { get; set; }

        public ReadOrderTourDTO? Tour { get; set; }

        public List<ReadOrderDetailDTO> OrderDetails { get; set; } = new List<ReadOrderDetailDTO>();

        public List<ReadTicketDTO> Tickets { get; set; } = new List<ReadTicketDTO>();

        public OrderReviewDTO? Review { get; set; }
    }

    public class OrderReviewDTO
    {
        public int Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ReadOrderScheduleDTO
    {
        public int Id { get; set; }

        public int TourId { get; set; }

        public DateTime DepartureDate { get; set; }

        public DateTime ReturnDate { get; set; }

        public string? Note { get; set; }

        public ICollection<ReadOrderScheduleItineraryDTO>? TourScheduleItineraries { get; set; }

        public ICollection<ReadOrderScheduleTicketDTO>? TourScheduleTickets { get; set; }
    }

    public class ReadOrderScheduleTicketDTO
    {
        public int Id { get; set; }
        public int ScheduleId { get; set; }
        public int TicketTypeId { get; set; }
        public long Price { get; set; }
        public int Quantity { get; set; }
        public int? SoldQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public bool? IsActive { get; set; }
        public string? Note { get; set; }
        public ReadPromotionDTO? Promotion { get; set; }
    }

    public class ReadOrderScheduleItineraryDTO
    {
        public int Id { get; set; }
        public int ScheduleId { get; set; }
        public DateTime ItineraryDate { get; set; }
        public int DayNumber { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        [Required(ErrorMessage = "StartDuration is required")]
        public TimeOnly? StartDuration { get; set; }

        [Required(ErrorMessage = "EndDuration is required")]
        public TimeOnly? EndDuration { get; set; }
        public string? LocationName { get; set; }
        public double? LocationLat { get; set; }
        public double? LocationLng { get; set; }
        public int? TourismInfoId { get; set; }

    }

    public class ReadOrderTourDTO
    {
        public int Id { get; set; }

        public int OperatorId { get; set; }

        public int CategoryId { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public string? Status { get; set; }

        public string? ImageUrl { get; set; }

        public string? Country { get; set; }

        public string? City { get; set; }

        public string? Address { get; set; }

        public double? AverageStar { get; set; }

        public List<TourReviewDetailDTO>? Reviews { get; set; }
    }

    public class TourReviewDetailDTO
    {
        public int Id { get; set; }
        public int CustomerId { get; set; } 
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public abstract class BaseOrderDTO
    {
        public int? CustomerId { get; set; }

        public int ScheduleId { get; set; }

        public int? TotalQuantity { get; set; }

        public int? TicketCount { get; set; }

        public long? DiscountValue { get; set; }

        public long FinalAmount { get; set; }

        public string? Note { get; set; }

        public string? Status { get; set; }

        public string? InviteToken { get; set; }
    }

    public class CreateOrderDTO : BaseOrderDTO
    {
        [StringLength(50, MinimumLength = 3, ErrorMessage = "VoucherCode must be between 3 and 50 characters")]
        public string? VoucherCode { get; set; }

        [Required] public List<CreateOrderDetailDTO> OrderDetails { get; set; } = new List<CreateOrderDetailDTO>();
    }
    public class UpdateOrderDTO : BaseOrderDTO
    {
    }

    public class CreateOrderDetailDTO
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "TourScheduleTicketId must be greater than 0")]
        public int TourScheduleTicketId { get; set; }

        public int? TicketTypeId { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
        public long? UnitPrice { get; set; }

        [Required]
        public List<CreateTicketDTO> Tickets { get; set; } = new List<CreateTicketDTO>();
    }

    public class ReadOrderDetailDTO
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int TicketTypeId { get; set; }
        public int TourScheduleTicketId { get; set; }
        public int Quantity { get; set; }
        public long UnitPrice { get; set; }
        public long TotalPrice { get; set; }
        public List<ReadTicketDTO> Tickets { get; set; } = new List<ReadTicketDTO>();
    }

    public class CheckBookingRequest
    {
        public int CustomerId { get; set; }
        public List<int> ScheduleIds { get; set; } = new List<int>();
    }
    public class CheckBookingTour
    {
        public List<int> ScheduleIds { get; set; } = new List<int>();
    }
}
