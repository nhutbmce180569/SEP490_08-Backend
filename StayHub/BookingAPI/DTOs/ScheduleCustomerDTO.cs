namespace BookingAPI.DTOs;

public class ScheduleCustomerDTO
{
    public int TicketId { get; set; }
    public int OrderId { get; set; }
    public int? UserId { get; set; }
    public string AttendeeName { get; set; } = null!;
    public string IdCard { get; set; } = null!;
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? CheckInStatus { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
}
