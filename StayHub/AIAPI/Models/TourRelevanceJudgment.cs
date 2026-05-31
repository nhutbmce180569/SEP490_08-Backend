namespace AIAPI.Models;

public class TourRelevanceJudgment
{
    public int Id { get; set; }
    public string ProfileSignature { get; set; } = null!;
    public string? ProfileQueryKey { get; set; }
    public int TourId { get; set; }
    public int RelevanceGrade { get; set; }
    public string Source { get; set; } = "expert";
    public string? JudgeId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
