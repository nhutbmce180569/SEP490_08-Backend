namespace AIAPI.Recommender;

/// <summary>
/// Aggregation strategies for group tour ranking (paper baselines + proposed method).
/// </summary>
public static class AggregationStrategies
{
    public const string CafhrFair = "cafhr_fair";
    public const string MeanUtility = "mean_utility";
    public const string LeastMisery = "least_misery";
    public const string BordaCount = "borda_count";
    public const string ContentOnly = "content_only";
    public const string CafhrNoKnowledge = "cafhr_no_knowledge";
    public const string CafhrNoPenalty = "cafhr_no_penalty";
    public const string PopularityWeighted = "popularity_weighted";
    public const string MgrsFair = "mgrs_fair";

    public static IReadOnlyList<BaselineDefinition> GetAll() =>
    [
        new BaselineDefinition(CafhrFair, "Hybrid Fair Utility",
            "Seed scorer: U = α·min_p u_p + (1-α)·mean_p u_p with min-persona penalty (τ=0.35)."),
        new BaselineDefinition(MeanUtility, "Mean Utility",
            "U = mean_p u_p — ignores least-satisfied member (strawman for fairness comparison)."),
        new BaselineDefinition(LeastMisery, "Least Misery",
            "U = min_p u_p — classic group recommender baseline (max-min welfare)."),
        new BaselineDefinition(BordaCount, "Borda Count",
            "Rank fusion: each persona ranks tours; Borda points summed across personas."),
        new BaselineDefinition(ContentOnly, "Content-Only",
            "Primary-persona utility only; non-primary personas receive u_p=0 (paper baseline)."),
        new BaselineDefinition(PopularityWeighted, "Popularity-Weighted Content",
            "0.6·ω̂(t) + 0.4·s₁,primary with hard filters; no group aggregation."),
        new BaselineDefinition(MgrsFair, "Proposed EC-FHGR / MGRS-Fair",
            "Evidence-cited hybrid scoring plus iterative egalitarian swap re-ranking on persona utilities (Sacharidis et al.)."),
        new BaselineDefinition(CafhrNoKnowledge, "CAFHR w/o Knowledge",
            "CAFHR fair aggregation with cultural_fit dimension disabled (ablation)."),
        new BaselineDefinition(CafhrNoPenalty, "FCAHR w/o Penalty",
            "CAFHR fair aggregation with min-persona penalty disabled (γ=0).")
    ];
}

public record BaselineDefinition(string Key, string Name, string Description);

public static class FairnessFormalization
{
    /// <summary>U_fair(t) = α · min_p u_p(t) + (1-α) · (1/|P|) Σ u_p(t)</summary>
    public static float CafhrUtility(IReadOnlyDictionary<string, float> personaUtilities, float alpha)
    {
        if (personaUtilities.Count == 0)
        {
            return 0f;
        }

        var min = personaUtilities.Values.Min();
        var mean = personaUtilities.Values.Average();
        return alpha * min + (1f - alpha) * mean;
    }

    public static float MeanUtility(IReadOnlyDictionary<string, float> personaUtilities) =>
        personaUtilities.Count == 0 ? 0f : personaUtilities.Values.Average();

    public static float LeastMiseryUtility(IReadOnlyDictionary<string, float> personaUtilities) =>
        personaUtilities.Count == 0 ? 0f : personaUtilities.Values.Min();

    /// <summary>Dissatisfaction variance: Var_p(1 - u_p)</summary>
    public static float DissatisfactionVariance(IReadOnlyDictionary<string, float> personaUtilities)
    {
        if (personaUtilities.Count <= 1)
        {
            return 0f;
        }

        var dissatisfactions = personaUtilities.Values.Select(u => 1f - u).ToList();
        var mean = dissatisfactions.Average();
        return dissatisfactions.Sum(d => (d - mean) * (d - mean)) / dissatisfactions.Count;
    }

    /// <summary>Envy gap: max_p u_p - min_p u_p</summary>
    public static float EnvyGap(IReadOnlyDictionary<string, float> personaUtilities)
    {
        if (personaUtilities.Count == 0)
        {
            return 0f;
        }

        return personaUtilities.Values.Max() - personaUtilities.Values.Min();
    }

    public static float ApplyMinPersonaPenalty(float utility, float minPersona, float threshold, float penaltyFactor = 0.75f) =>
        minPersona < threshold ? utility * penaltyFactor : utility;
}
