using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIAPI.Models;

[Table("AILogs")]
public class AILog
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public long? Budget { get; set; }

    public int? Days { get; set; }

    public string? ResultIds { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
