namespace BookingAPI.DTOs;

public class BatchUserProfileDTO
{
    public int Id { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? AvatarUrl { get; set; }
}
