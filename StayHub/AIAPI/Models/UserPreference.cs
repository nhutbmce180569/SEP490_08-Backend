using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIAPI.Models;

[Table("UserPreferences")]
public class UserPreference
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? InterestTags { get; set; }

    public long? MinBudget { get; set; }

    public long? MaxBudget { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
