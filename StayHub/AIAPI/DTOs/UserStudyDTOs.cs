namespace AIAPI.DTOs;

public class UserStudyProtocolDTO
{
    public string ProtocolVersion { get; set; } = "1.0";
    public string Title { get; set; } = "";
    public string Objective { get; set; } = "";
    public int TargetParticipants { get; set; } = 52;
    public int TargetGroups { get; set; } = 18;
    public int ScenarioCount { get; set; } = 18;
    public int ToursPerList { get; set; } = 5;
    public int EstimatedMinutes { get; set; } = 15;
    public List<LikertScaleDefinitionDTO> Scales { get; set; } = new();
    public List<string> ComparisonPairs { get; set; } = new();
    public List<string> Instructions { get; set; } = new();
    public UserStudyPowerAnalysisDTO? PowerAnalysis { get; set; }
}

public class UserStudyPowerAnalysisDTO
{
    public string Design { get; set; } = "";
    public int TargetParticipants { get; set; }
    public int ScenarioCount { get; set; }
    public int TotalPreferenceObservations { get; set; }
    public double AssumedCohensD { get; set; }
    public double Alpha { get; set; }
    public double TargetPower { get; set; }
    public int GPowerMinimumPairs { get; set; }
    public bool MeetsPowerTarget { get; set; }
    public string Justification { get; set; } = "";
}

public class LikertScaleDefinitionDTO
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public int Min { get; set; } = 1;
    public int Max { get; set; } = 7;
    public string MinLabel { get; set; } = "";
    public string MaxLabel { get; set; } = "";
}

public class UserStudyScenarioDTO
{
    public int ScenarioId { get; set; }
    public string Title { get; set; } = "";
    public string Vignette { get; set; } = "";
    public string ComparisonPair { get; set; } = "";
    public string ProfileQueryKey { get; set; } = "";
    /// <summary>solo | family | friend | couple</summary>
    public string GroupType { get; set; } = "";
}

public class UserStudyComparisonDTO
{
    public int AssignmentId { get; set; }
    public int ScenarioId { get; set; }
    public string SessionId { get; set; } = "";
    public UserStudyScenarioDTO Scenario { get; set; } = new();
    public UserStudyBlindListDTO ListA { get; set; } = new();
    public UserStudyBlindListDTO ListB { get; set; } = new();
    public bool AlreadySubmitted { get; set; }
}

public class UserStudyBlindListDTO
{
    public string Label { get; set; } = "";
    public List<UserStudyTourItemDTO> Tours { get; set; } = new();
}

public class UserStudyTourItemDTO
{
    public int TourId { get; set; }
    public string Name { get; set; } = "";
    public string? City { get; set; }
    public long? MinPrice { get; set; }
    public int? DurationDays { get; set; }
    public string Highlight { get; set; } = "";
}

public class SubmitUserStudyResponseDTO
{
    public int AssignmentId { get; set; }
    public string SessionId { get; set; } = null!;
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
}

public class SubmitUserStudyResponseResultDTO
{
    public int ResponseId { get; set; }
    public string Message { get; set; } = "";
}

public class UserStudySummaryDTO
{
    public string ProtocolVersion { get; set; } = "";
    public int TotalResponses { get; set; }
    public int UniqueParticipants { get; set; }
    public int TargetParticipants { get; set; } = 52;
    public int TargetScenariosPerParticipant { get; set; } = 18;
    public float CompletionRate { get; set; }
    public List<UserStudyComparisonStatsDTO> ComparisonStats { get; set; } = new();
    public List<UserStudyGroupTypeStatsDTO> GroupTypeStats { get; set; } = new();
    public UserStudyFcahrAggregateDTO FcahrAggregate { get; set; } = new();
    public UserStudyPowerAnalysisDTO PowerAnalysis { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class UserStudyGroupTypeStatsDTO
{
    public string GroupType { get; set; } = "";
    public int ResponseCount { get; set; }
    public float FcahrPreferenceRate { get; set; }
    public float MeanGroupFairnessAdvantage { get; set; }
}

public class UserStudyComparisonStatsDTO
{
    public string ComparisonPair { get; set; } = "";
    public int ResponseCount { get; set; }
    public float FcahrPreferenceRate { get; set; }
    public float MeanFairnessDelta { get; set; }
    public float MeanSatisfactionDelta { get; set; }
    public float MeanGroupFairnessDelta { get; set; }
    public float PreferenceSignTestPApprox { get; set; }
}

public class UserStudyFcahrAggregateDTO
{
    public float OverallPreferenceRate { get; set; }
    public float MeanFairnessAdvantage { get; set; }
    public float MeanSatisfactionAdvantage { get; set; }
    public float MeanGroupFairnessAdvantage { get; set; }
}
