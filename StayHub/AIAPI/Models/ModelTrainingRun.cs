namespace AIAPI.Models;

public class ModelTrainingRun
{
    public int Id { get; set; }
    public string ModelName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int TourCount { get; set; }
    public int TourismCount { get; set; }
    public int InteractionCount { get; set; }
    public double? IntentAccuracy { get; set; }
    public string? Message { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
