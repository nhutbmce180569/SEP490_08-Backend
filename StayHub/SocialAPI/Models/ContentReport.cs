using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialAPI.Models;

public class ContentReport
{
    public int Id { get; set; }

    public int ReporterId { get; set; }

    public string ContentType { get; set; } = null!;

    public int TargetId { get; set; }

    public string Reason { get; set; } = null!;

    public string? Details { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    [NotMapped]
    public string? ContentText { get; set; }

    [NotMapped]
    public string? ContentImageUrl { get; set; }

    [NotMapped]
    public string? ReporterName { get; set; }

    [NotMapped]
    public string? ReporterEmail { get; set; }
}
