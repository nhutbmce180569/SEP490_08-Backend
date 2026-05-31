using System.ComponentModel.DataAnnotations;

namespace AIAPI.DTOs;

public class ChatRequestDTO
{
    [Required(ErrorMessage = "Message is required.")]
    [StringLength(2000, MinimumLength = 2, ErrorMessage = "Message must be between 2 and 2000 characters.")]
    public string Message { get; set; } = null!;

    [StringLength(64, ErrorMessage = "SessionId cannot exceed 64 characters.")]
    public string? SessionId { get; set; }
}

public class NaturalLanguageSearchRequestDTO
{
    [Required(ErrorMessage = "Query is required.")]
    [StringLength(1000, MinimumLength = 2, ErrorMessage = "Query must be between 2 and 1000 characters.")]
    public string Query { get; set; } = null!;

    [Range(1, 50, ErrorMessage = "Top must be between 1 and 50.")]
    public int Top { get; set; } = 10;

    public int? CategoryId { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public long? MinPrice { get; set; }
    public long? MaxPrice { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DurationDays { get; set; }
    public int? GroupSize { get; set; }
}

public class TourConsultationRequestDTO
{
    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters.")]
    public string? Country { get; set; }

    [Range(0, long.MaxValue, ErrorMessage = "MinPrice must be non-negative.")]
    public long? MinPrice { get; set; }

    [Range(0, long.MaxValue, ErrorMessage = "MaxPrice must be non-negative.")]
    public long? MaxPrice { get; set; }

    public DateTime? PreferredStartDate { get; set; }
    public DateTime? PreferredEndDate { get; set; }

    [Range(1, 365, ErrorMessage = "DurationDays must be between 1 and 365.")]
    public int? DurationDays { get; set; }

    [Range(1, 500, ErrorMessage = "GroupSize must be between 1 and 500.")]
    public int? GroupSize { get; set; }

    public int? CategoryId { get; set; }

    [StringLength(500, ErrorMessage = "TravelStyle cannot exceed 500 characters.")]
    public string? TravelStyle { get; set; }

    [Range(1, 30, ErrorMessage = "Top must be between 1 and 30.")]
    public int Top { get; set; } = 8;
}

public class LogInteractionRequestDTO
{
    [Required]
    [Range(1, int.MaxValue)]
    public int TourId { get; set; }

    [Required]
    [RegularExpression("^(view|click|wishlist|booking|chat_recommend)$",
        ErrorMessage = "InteractionType must be view, click, wishlist, booking, or chat_recommend.")]
    public string InteractionType { get; set; } = null!;

    [StringLength(64)]
    public string? SessionId { get; set; }
}

public class TourRecommendationItemDTO
{
    public int TourId { get; set; }
    public string Name { get; set; } = "";
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? ImageUrl { get; set; }
    public double? AverageStar { get; set; }
    public long? MinPrice { get; set; }
    public int? DurationDays { get; set; }
    public float Score { get; set; }
    public string Reason { get; set; } = "";
    public List<string> MatchReasons { get; set; } = new();
    public TourScoreBreakdownDTO? ScoreBreakdown { get; set; }
}

public class TourScoreBreakdownDTO
{
    public float FairnessScore { get; set; }
    public float MinPersonaScore { get; set; }
    public float MeanPersonaScore { get; set; }
    public Dictionary<string, float> PersonaScores { get; set; } = new();
    public Dictionary<string, float> DimensionScores { get; set; } = new();
    public float EnvyGap { get; set; }
    public float DissatisfactionVariance { get; set; }
    public string AggregationFormula { get; set; } = "";
}

public class TourSearchResultItemDTO : TourRecommendationItemDTO
{
    public float SemanticScore { get; set; }
    public string? Snippet { get; set; }
}

public class TourismInsightDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Description { get; set; }
    public string? City { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }
    public float RelevanceScore { get; set; }
    public string? AuthorityLevel { get; set; }
    public string? KnowledgeProvider { get; set; }
}

public class ChatResponseDTO
{
    public string SessionId { get; set; } = "";
    public string Intent { get; set; } = "";
    public float IntentConfidence { get; set; }
    public string Reply { get; set; } = "";
    public ParsedQueryDTO ParsedQuery { get; set; } = new();
    public List<TourSearchResultItemDTO> RecommendedTours { get; set; } = new();
    public List<TourismInsightDTO> TourismInsights { get; set; } = new();
    public List<string> SuggestedQuestions { get; set; } = new();
    public bool UsedPersonalization { get; set; }
}

public class ParsedQueryDTO
{
    public string? City { get; set; }
    public string? Country { get; set; }
    public long? MinPrice { get; set; }
    public long? MaxPrice { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DurationDays { get; set; }
    public int? GroupSize { get; set; }
    public int? CategoryId { get; set; }
}

public class ModelTrainingStatusDTO
{
    public bool IsReady { get; set; }
    public DateTime? LastTrainedAt { get; set; }
    public int TourCatalogCount { get; set; }
    public int TourismKnowledgeCount { get; set; }
    public int InteractionCount { get; set; }
    public double? IntentModelAccuracy { get; set; }
    public List<ModelTrainingRunDTO> RecentRuns { get; set; } = new();
}

public class ModelTrainingRunDTO
{
    public int Id { get; set; }
    public string ModelName { get; set; } = "";
    public string Status { get; set; } = "";
    public int TourCount { get; set; }
    public int TourismCount { get; set; }
    public int InteractionCount { get; set; }
    public double? IntentAccuracy { get; set; }
    public string? Message { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
