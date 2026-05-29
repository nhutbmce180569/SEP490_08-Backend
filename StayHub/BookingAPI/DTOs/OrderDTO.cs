using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookingAPI.DTOs
{
    public class ReadOrderDTO
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }

        public int ScheduleId { get; set; }

        public int TicketCount { get; set; }

        public long? DiscountValue { get; set; }

        public long FinalAmount { get; set; }

        public string? Note { get; set; }

        public string? Status { get; set; }
        public DateTime? OrderedAt { get; set; }

        public string? InviteToken { get; set; }

        public ReadOrderScheduleDTO? Schedule { get; set; }

        public ReadOrderTourDTO? Tour { get; set; }

        public List<ReadTicketDTO> Tickets { get; set; } = new List<ReadTicketDTO>();

        public OrderReviewDTO? Review { get; set; }
    }

    public class OrderReviewDTO
    {
        public int Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class ReadOrderScheduleDTO
    {
        public int Id { get; set; }

        public int TourId { get; set; }

        public DateTime DepartureDate { get; set; }

        public DateTime ReturnDate { get; set; }

        public long Price { get; set; }

        public int MaxCapacity { get; set; }

        public int SoldQuantity { get; set; }

        public int AvailableSeats { get; set; }

        public string? Note { get; set; }

        public ICollection<ReadOrderScheduleItineraryDTO>? TourScheduleItineraries { get; set; }
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
    }

    public abstract class BaseOrderDTO
    {
        public int? CustomerId { get; set; }

        public int ScheduleId { get; set; }

        public int? TicketCount { get; set; }

        public long? DiscountValue { get; set; }

        public long FinalAmount { get; set; }

        public string? Note { get; set; }

        public string? Status { get; set; }

        public string? InviteToken { get; set; }
    }

    public class CreateOrderDTO : BaseOrderDTO
    {
        [Required] public List<CreateTicketDTO> Tickets { get; set; } = new List<CreateTicketDTO>();
    }
    public class UpdateOrderDTO : BaseOrderDTO
    {
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
