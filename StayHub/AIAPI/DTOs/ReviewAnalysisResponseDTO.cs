using System.Text.Json.Serialization;

namespace AIAPI.DTOs;

public class ReviewAnalysisResponseDTO
{
    [JsonPropertyName("sentiment")]
    public string Sentiment { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}
