using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIAPI.Models;

[Table("UserTourInteractions")]
public class UserTourInteraction
{
    [Key]
    public int Id { get; set; }

    public int? CustomerId { get; set; }

    public int TourId { get; set; }

    [MaxLength(50)]
    public string InteractionType { get; set; } = null!;

    public double Weight { get; set; }

    [MaxLength(64)]
    public string? SessionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
