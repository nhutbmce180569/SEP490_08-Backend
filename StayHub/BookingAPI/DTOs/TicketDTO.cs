using System;
using System.ComponentModel.DataAnnotations;

namespace BookingAPI.DTOs
{
    public class ReadTicketDTO
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int OrderDetailId { get; set; }

        public int? UserId { get; set; }

        public int TicketTypeId { get; set; }

        public string? TicketTypeName { get; set; }

        public string AttendeeName { get; set; } = null!;

        public string IdCard { get; set; } = null!;

        public DateOnly? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? Nationality { get; set; }

        public string? QrCode { get; set; }

        public string? CheckInStatus { get; set; }
    }
    public abstract class BaseTicketDTO
    {
        public int? UserId { get; set; }

        public int? TicketTypeId { get; set; }

        [Required(ErrorMessage = "Attendee name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Attendee name must be between 2 and 100 characters.")]
        public string AttendeeName { get; set; } = null!;

        [Required(ErrorMessage = "ID or Passport number is required.")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "ID/Passport must be between 5 and 20 characters.")]
        [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "ID/Passport can only contain alphanumeric characters and hyphens.")]
        public string IdCard { get; set; } = null!;

        public DateOnly? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? Nationality { get; set; }

        public string? QrCode { get; set; }

        public string? CheckInStatus { get; set; }
    }

    public class CreateTicketDTO : BaseTicketDTO
    {

    }
    public class UpdateTicketDTO : BaseTicketDTO
    {

    }

    public class CheckInRequestDTO
    {
        public string QrCode { get; set; } = string.Empty;
    }

    public class TicketTypeResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
    }

    public class CheckInResultDTO
    {
        public int TicketId { get; set; }
        public string AttendeeName { get; set; } = null!;
        public string TicketTypeName { get; set; } = null!;
        public string CheckInStatus { get; set; } = null!;
        public int ScheduleId { get; set; }
        public DateTime DepartureDate { get; set; }
    }
}
