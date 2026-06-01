namespace AIAPI.Models;

public class UserStudyResponse
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string SessionId { get; set; } = null!;
    public int ScenarioId { get; set; }
    public string PreferredList { get; set; } = null!;
    public int FairnessListA { get; set; }
    public int FairnessListB { get; set; }
    public int SatisfactionListA { get; set; }
    public int SatisfactionListB { get; set; }
    public int GroupFairnessListA { get; set; }
    public int GroupFairnessListB { get; set; }
    public int WouldBookListA { get; set; }
    public int WouldBookListB { get; set; }
    public string? AgeGroup { get; set; }
    public string? TravelExperience { get; set; }
    public string? OpenComment { get; set; }
    public string ResponseSource { get; set; } = "human";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserStudyAssignment Assignment { get; set; } = null!;
}
