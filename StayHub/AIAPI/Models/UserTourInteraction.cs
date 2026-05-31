namespace AIAPI.Models;

public class UserTourInteraction
{
    public int Id { get; set; }
    public int? CustomerId { get; set; }
    public int TourId { get; set; }
    public string InteractionType { get; set; } = null!;
    public double Weight { get; set; } = 1;
    public string? SessionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
