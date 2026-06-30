using System.ComponentModel.DataAnnotations;

namespace AIAPI.DTOs;

/// <summary>Đi một mình | Gia đình | Couple | Nhóm bạn</summary>
public static class TravelCompanionTypes
{
    public const string Solo = "solo";
    public const string Family = "family";
    public const string Couple = "couple";
    public const string Group = "group";
}

public static class TravelerNationalityTypes
{
    public const string Vietnamese = "vietnamese";
    public const string Foreigner = "foreigner";
}

public static class TravelPaceTypes
{
    public const string Relaxed = "relaxed";
    public const string Moderate = "moderate";
    public const string Packed = "packed";
}

public class TourPreferenceQuestionnaireDTO
{
    [Required(ErrorMessage = "CompanionType is required.")]
    [RegularExpression("^(solo|family|couple|group)$", ErrorMessage = "CompanionType must be solo, family, couple, or group.")]
    public string CompanionType { get; set; } = null!;

    [Required(ErrorMessage = "PreferredStartDate is required.")]
    public DateTime PreferredStartDate { get; set; }

    public DateTime? PreferredEndDate { get; set; }

    [Range(0, long.MaxValue)]
    public long? MaxBudgetPerPerson { get; set; }

    [Required(ErrorMessage = "TravelPace is required.")]
    [RegularExpression("^(relaxed|moderate|packed)$", ErrorMessage = "TravelPace must be relaxed, moderate, or packed.")]
    public string TravelPace { get; set; } = TravelPaceTypes.Moderate;

    [Range(1, 20)]
    public int AdultCount { get; set; } = 1;

    [Range(0, 20)]
    public int ChildrenCount { get; set; } = 0;

    [Range(0, 20)]
    public int ElderlyCount { get; set; } = 0;

    [Required]
    [MinLength(1, ErrorMessage = "Select at least one travel interest.")]
    public List<string> TravelInterests { get; set; } = new();

    [Required]
    [RegularExpression("^(vietnamese|foreigner)$", ErrorMessage = "NationalityType must be vietnamese or foreigner.")]
    public string NationalityType { get; set; } = null!;

    [StringLength(100)]
    public string? PreferredCity { get; set; }

    [StringLength(100)]
    public string? PreferredCountry { get; set; }

    [Range(1, 30)]
    public int Top { get; set; } = 10;

    [StringLength(64)]
    public string? SessionId { get; set; }
}

public class QuestionnaireFieldDTO
{
    public string FieldKey { get; set; } = "";
    public string Label { get; set; } = "";
    public string InputType { get; set; } = "";
    public bool Required { get; set; }
    public List<QuestionnaireOptionDTO>? Options { get; set; }
    public string? Hint { get; set; }
}

public class QuestionnaireOptionDTO
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
}

public class StandardQuestionnaireDTO
{
    public string Version { get; set; } = "1.0";
    public List<QuestionnaireFieldDTO> Questions { get; set; } = new();
}

public class WeatherAdviceDTO
{
    public string City { get; set; } = "";
    public string DataSource { get; set; } = "";
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public double? AvgMaxTempC { get; set; }
    public double? AvgMinTempC { get; set; }
    public double? TotalRainMm { get; set; }
    public string Summary { get; set; } = "";
    public string ImpactOnTours { get; set; } = "";
}

public class ScheduleAvailabilityDTO
{
    public bool HasToursInPreferredWindow { get; set; }
    public string PreferredStartDate { get; set; } = "";
    public string PreferredEndDate { get; set; } = "";
    public string CustomerMessage { get; set; } = "";
}

public class PersonalizedRecommendationResponseDTO
{
    public string SessionId { get; set; } = "";
    public string Summary { get; set; } = "";
    public TourPreferenceQuestionnaireDTO AppliedProfile { get; set; } = new();
    public ScheduleAvailabilityDTO? ScheduleAvailability { get; set; }
    public WeatherAdviceDTO? WeatherAdvice { get; set; }
    public List<string> GeneralTips { get; set; } = new();
    public List<string> ForeignVisitorTips { get; set; } = new();
    public List<string> ElderlyCompanionTips { get; set; } = new();
    public List<string> ChildrenCompanionTips { get; set; } = new();
    public List<TourRecommendationItemDTO> RecommendedTours { get; set; } = new();
    public List<TourRecommendationItemDTO> NearbyScheduleTours { get; set; } = new();
    public List<TourismInsightDTO> RelatedInsights { get; set; } = new();
    public RecommenderTransparencyDTO RecommenderMeta { get; set; } = new();
    public List<CulturalFactDTO> CulturalFacts { get; set; } = new();
}

public class CulturalFactDTO
{
    public string Fact { get; set; } = "";
    public string SourceName { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string AuthorityLevel { get; set; } = "";
    public string Provider { get; set; } = "";
    public string? City { get; set; }
}

public class RecommenderTransparencyDTO
{
    public string ModelVersion { get; set; } = "";
    public string ModelFamily { get; set; } = "";
    public string MethodologySummary { get; set; } = "";
    public string AggregationFormula { get; set; } = "";
    public float FairnessAlpha { get; set; }
    public List<string> PersonaTypesUsed { get; set; } = new();
    public List<KnowledgeSourceDTO> KnowledgeSources { get; set; } = new();
    public List<AcademicReferenceDTO> AcademicReferences { get; set; } = new();
    public Dictionary<string, float> DimensionWeights { get; set; } = new();
}

public class KnowledgeSourceDTO
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Authority { get; set; } = "";
}

public class AcademicReferenceDTO
{
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string Authors { get; set; } = "";
    public string Venue { get; set; } = "";
    public int Year { get; set; }
    public string Url { get; set; } = "";
    public string? Doi { get; set; }
    public string UsedFor { get; set; } = "";
}

public class ScoringModelDocumentationDTO
{
    public RecommenderTransparencyDTO Specification { get; set; } = new();
    public string MethodologySummary { get; set; } = "";
    public string PaperTitleSuggestion { get; set; } = "";
    public Dictionary<string, string> FormalDefinitions { get; set; } = new();
    public List<AcademicReferenceDTO> AcademicReferences { get; set; } = new();
    public List<BaselineInfoDTO> Baselines { get; set; } = new();
}
