namespace AIAPI.Models;

public class UserStudyAssignment
{
    public int Id { get; set; }
    public string SessionId { get; set; } = null!;
    public int ScenarioId { get; set; }
    public string StrategyForListA { get; set; } = null!;
    public string StrategyForListB { get; set; } = null!;
    public string ComparisonPair { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
