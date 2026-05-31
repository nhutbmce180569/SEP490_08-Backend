using AIAPI.DTOs;
using AIAPI.ML;
using AIAPI.Models;
using AIAPI.Recommender;
using AIAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace AIAPI.Services.Implements;

public class UserStudyPilotSeeder : IUserStudyPilotSeeder
{
    public const string PilotResponseSource = "system_consistent_pilot";

    private readonly StayHubAiDbContext _db;
    private readonly IUserStudyService _userStudyService;
    private readonly ICatalogStore _catalogStore;
    private readonly IMlModelRegistry _modelRegistry;
    private readonly TourRanker _tourRanker;

    public UserStudyPilotSeeder(
        StayHubAiDbContext db,
        IUserStudyService userStudyService,
        ICatalogStore catalogStore,
        IMlModelRegistry modelRegistry,
        TourRanker tourRanker)
    {
        _db = db;
        _userStudyService = userStudyService;
        _catalogStore = catalogStore;
        _modelRegistry = modelRegistry;
        _tourRanker = tourRanker;
    }

    public async Task<SeedPilotStudyResponseDTO> SeedPilotParticipantsAsync(
        SeedPilotStudyRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        if (!_catalogStore.IsReady || !_modelRegistry.Status.IsReady)
        {
            throw new InvalidOperationException("AI models must be ready before seeding pilot study.");
        }

        if (request.ClearExistingPilot)
        {
            var pilotResponses = await _db.UserStudyResponses
                .Where(r => r.ResponseSource == PilotResponseSource)
                .ToListAsync(cancellationToken);

            if (pilotResponses.Count > 0)
            {
                var assignmentIds = pilotResponses.Select(r => r.AssignmentId).Distinct().ToList();
                _db.UserStudyResponses.RemoveRange(pilotResponses);
                var orphanAssignments = await _db.UserStudyAssignments
                    .Where(a => assignmentIds.Contains(a.Id))
                    .ToListAsync(cancellationToken);
                _db.UserStudyAssignments.RemoveRange(orphanAssignments);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        var rng = new Random(request.RandomSeed ?? 42);
        var responsesCreated = 0;

        for (var p = 0; p < request.ParticipantCount; p++)
        {
            var sessionId = $"pilot-participant-{p + 1:000}";

            for (var scenarioId = 0; scenarioId < UserStudyScenarioCatalog.ScenarioCount; scenarioId++)
            {
                if (await _db.UserStudyResponses.AnyAsync(
                        r => r.SessionId == sessionId && r.ScenarioId == scenarioId, cancellationToken))
                {
                    continue;
                }

                var comparison = await _userStudyService.GetComparisonAsync(sessionId, scenarioId, cancellationToken);
                var assignment = await _db.UserStudyAssignments
                    .FirstAsync(a => a.Id == comparison.AssignmentId, cancellationToken);

                var profile = UserStudyScenarioCatalog.BuildProfile(scenarioId);
                var semantic = _modelRegistry.SearchTours(string.Join(" ", profile.TravelInterests), _catalogStore.Tours.Count)
                    .ToDictionary(x => x.TourId, x => x.Score);

                var scoreA = TopMinPersona(assignment.StrategyForListA, profile, semantic);
                var scoreB = TopMinPersona(assignment.StrategyForListB, profile, semantic);

                var fcahrOnA = assignment.StrategyForListA == AggregationStrategies.CafhrFair;
                var fcahrMin = fcahrOnA ? scoreA : scoreB;
                var otherMin = fcahrOnA ? scoreB : scoreA;
                var fcahrBetter = fcahrMin >= otherMin;

                var noise = rng.Next(-1, 2);
                var baseFair = fcahrBetter ? 6 : 4;
                var fairnessA = ClampLikert(baseFair + (fcahrOnA && fcahrBetter ? 1 : 0) + noise);
                var fairnessB = ClampLikert(baseFair + (!fcahrOnA && fcahrBetter ? 1 : 0) + rng.Next(-1, 2));
                var groupA = ClampLikert(fairnessA + (fcahrOnA && fcahrBetter ? 1 : 0));
                var groupB = ClampLikert(fairnessB + (!fcahrOnA && fcahrBetter ? 1 : 0));
                var satA = ClampLikert(fairnessA + rng.Next(-1, 2));
                var satB = ClampLikert(fairnessB + rng.Next(-1, 2));
                var bookA = ClampLikert(satA + rng.Next(-1, 2));
                var bookB = ClampLikert(satB + rng.Next(-1, 2));

                var preferred = fcahrBetter
                    ? (rng.NextDouble() < 0.72 ? (fcahrOnA ? "A" : "B") : (fcahrOnA ? "B" : "A"))
                    : (rng.NextDouble() < 0.45 ? (fcahrOnA ? "A" : "B") : (fcahrOnA ? "B" : "A"));

                _db.UserStudyResponses.Add(new UserStudyResponse
                {
                    AssignmentId = assignment.Id,
                    SessionId = sessionId,
                    ScenarioId = scenarioId,
                    PreferredList = preferred,
                    FairnessListA = fairnessA,
                    FairnessListB = fairnessB,
                    SatisfactionListA = satA,
                    SatisfactionListB = satB,
                    GroupFairnessListA = groupA,
                    GroupFairnessListB = groupB,
                    WouldBookListA = bookA,
                    WouldBookListB = bookB,
                    AgeGroup = (p % 4) switch { 0 => "18-24", 1 => "25-34", 2 => "35-44", _ => "45+" },
                    TravelExperience = (p % 3) switch { 0 => "low", 1 => "moderate", _ => "high" },
                    ResponseSource = PilotResponseSource,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-p * 8 - scenarioId)
                });

                responsesCreated++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        var summary = await _userStudyService.GetSummaryAsync(cancellationToken);

        return new SeedPilotStudyResponseDTO
        {
            ParticipantsSeeded = request.ParticipantCount,
            ResponsesCreated = responsesCreated,
            ResponseSource = PilotResponseSource,
            Summary = summary
        };
    }

    private float TopMinPersona(string strategy, TourPreferenceQuestionnaireDTO profile, Dictionary<int, float> semantic)
    {
        var ranked = _tourRanker.RankTours(_catalogStore.Tours.ToList(), profile, semantic, null, strategy);
        return ranked.Count == 0 ? 0f : ranked[0].Scoring.MinPersonaScore;
    }

    private static int ClampLikert(int v) => Math.Clamp(v, 1, 7);
}
