using System.ComponentModel.DataAnnotations;

namespace AIAPI.DTOs;

public class IntelligentChatRequestDTO
{
    [Required(ErrorMessage = "Message is required.")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 2000 characters.")]
    public string Message { get; set; } = null!;

    [StringLength(64, ErrorMessage = "SessionId cannot exceed 64 characters.")]
    public string? SessionId { get; set; }

    public List<IntelligentChatMessageDTO>? History { get; set; }
}

public class IntelligentChatMessageDTO
{
    [Required]
    [RegularExpression("^(user|model|function)$", ErrorMessage = "Role must be user, model, or function.")]
    public string Role { get; set; } = null!; // "user", "model", or "function"

    [Required]
    public string Content { get; set; } = null!;
}

public class IntelligentChatResponseDTO
{
    public string SessionId { get; set; } = "";
    public string Reply { get; set; } = "";
    public List<TourRecommendationItemDTO> RecommendedTours { get; set; } = new();
    public WeatherAdviceDTO? WeatherAdvice { get; set; }
    public List<CulturalFactDTO> CulturalFacts { get; set; } = new();
    public List<TourismInsightDTO> TourismInsights { get; set; } = new();
    public List<string> SuggestedQuestions { get; set; } = new();
}
