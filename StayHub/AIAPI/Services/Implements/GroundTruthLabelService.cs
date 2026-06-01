using AIAPI.DTOs;
using AIAPI.Models;
using AIAPI.Models.Catalog;
using AIAPI.Recommender;
using Microsoft.EntityFrameworkCore;

namespace AIAPI.Services.Implements;

public class GroundTruthLabelService : IGroundTruthLabelService
{
    private static readonly Dictionary<string, float> InteractionWeights = new(StringComparer.OrdinalIgnoreCase)
    {
        ["booking"] = 5f,
        ["book"] = 5f,
        ["wishlist"] = 4f,
        ["click"] = 2f,
        ["view"] = 1f,
        ["search"] = 0.5f
    };

    private readonly StayHubAiDbContext _db;

    public GroundTruthLabelService(StayHubAiDbContext db)
    {
        _db = db;
    }

    public async Task<GroundTruthBundle> BuildLabelsAsync(
        TourPreferenceQuestionnaireDTO profile,
        IReadOnlyList<TourCatalogItem> catalog,
        string mode,
        CancellationToken cancellationToken = default)
    {
        var normalizedMode = NormalizeMode(mode);
        var proxy = RelevanceLabeler.LabelTours(profile, catalog);
        var signature = ProfileSignatureHelper.Compute(profile);
        var queryKey = ProfileSignatureHelper.BuildQueryKey(profile);

        var expertRows = await _db.TourRelevanceJudgments
            .AsNoTracking()
            .Where(j => j.ProfileSignature == signature || j.ProfileQueryKey == queryKey)
            .ToListAsync(cancellationToken);

        var expert = expertRows
            .GroupBy(j => j.TourId)
            .ToDictionary(g => g.Key, g => g.Max(j => j.RelevanceGrade));

        var interactionPrior = await BuildInteractionPriorAsync(catalog, cancellationToken);

        var merged = new Dictionary<int, int>();
        foreach (var tour in catalog)
        {
            merged[tour.Id] = normalizedMode switch
            {
                GroundTruthModes.Expert => expert.GetValueOrDefault(tour.Id, 0),
                GroundTruthModes.InteractionAugmented => MergeInteractionAugmented(
                    proxy.GetValueOrDefault(tour.Id, 0),
                    interactionPrior.GetValueOrDefault(tour.Id, 0f)),
                GroundTruthModes.Hybrid => MergeHybrid(
                    proxy.GetValueOrDefault(tour.Id, 0),
                    expert.GetValueOrDefault(tour.Id, -1),
                    interactionPrior.GetValueOrDefault(tour.Id, 0f)),
                _ => proxy.GetValueOrDefault(tour.Id, 0)
            };
        }

        return new GroundTruthBundle
        {
            Mode = normalizedMode,
            ProfileSignature = signature,
            ProfileQueryKey = queryKey,
            Labels = merged,
            ExpertJudgmentCount = expert.Count,
            InteractionSignalCount = interactionPrior.Count(kv => kv.Value > 0),
            ProxyRelevantCount = proxy.Count(kv => kv.Value >= 2)
        };
    }

    public async Task<int> ImportJudgmentsAsync(
        IEnumerable<ExpertJudgmentInputDTO> judgments,
        CancellationToken cancellationToken = default)
    {
        var rows = judgments.Select(j => new TourRelevanceJudgment
        {
            ProfileSignature = j.ProfileSignature,
            ProfileQueryKey = j.ProfileQueryKey,
            TourId = j.TourId,
            RelevanceGrade = Math.Clamp(j.RelevanceGrade, 0, 3),
            Source = string.IsNullOrWhiteSpace(j.Source) ? "expert" : j.Source!,
            JudgeId = j.JudgeId,
            Notes = j.Notes,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        if (rows.Count == 0)
        {
            return 0;
        }

        _db.TourRelevanceJudgments.AddRange(rows);
        await _db.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }

    public async Task<IReadOnlyList<ExpertJudgmentDTO>> GetJudgmentsAsync(
        string? profileSignature,
        CancellationToken cancellationToken = default)
    {
        var query = _db.TourRelevanceJudgments.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(profileSignature))
        {
            query = query.Where(j => j.ProfileSignature == profileSignature || j.ProfileQueryKey == profileSignature);
        }

        return await query
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new ExpertJudgmentDTO
            {
                Id = j.Id,
                ProfileSignature = j.ProfileSignature,
                ProfileQueryKey = j.ProfileQueryKey,
                TourId = j.TourId,
                RelevanceGrade = j.RelevanceGrade,
                Source = j.Source,
                JudgeId = j.JudgeId,
                Notes = j.Notes,
                CreatedAt = j.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private static string NormalizeMode(string mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return GroundTruthModes.Proxy;
        }

        var m = mode.Trim().ToLowerInvariant();
        return m switch
        {
            GroundTruthModes.Hybrid => GroundTruthModes.Hybrid,
            GroundTruthModes.InteractionAugmented => GroundTruthModes.InteractionAugmented,
            GroundTruthModes.Expert => GroundTruthModes.Expert,
            _ => GroundTruthModes.Proxy
        };
    }

    private static int MergeHybrid(int proxy, int expert, float interactionScore)
    {
        if (expert >= 0)
        {
            return expert;
        }

        var boosted = proxy;
        if (interactionScore >= 8f && proxy >= 1)
        {
            boosted = Math.Min(3, proxy + 1);
        }

        return boosted;
    }

    private static int MergeInteractionAugmented(int proxy, float interactionScore)
    {
        if (interactionScore >= 12f)
        {
            return Math.Max(proxy, 2);
        }

        if (interactionScore >= 6f && proxy >= 1)
        {
            return Math.Min(3, proxy + 1);
        }

        return proxy;
    }

    private async Task<Dictionary<int, float>> BuildInteractionPriorAsync(
        IReadOnlyList<TourCatalogItem> catalog,
        CancellationToken cancellationToken)
    {
        var tourIds = catalog.Select(t => t.Id).ToHashSet();
        var interactions = await _db.UserTourInteractions
            .AsNoTracking()
            .Where(i => tourIds.Contains(i.TourId))
            .ToListAsync(cancellationToken);

        var scores = new Dictionary<int, float>();
        foreach (var group in interactions.GroupBy(i => i.TourId))
        {
            var score = group.Sum(i =>
            {
                var typeWeight = InteractionWeights.GetValueOrDefault(i.InteractionType, 1f);
                return (float)(typeWeight * i.Weight);
            });
            scores[group.Key] = score;
        }

        return scores;
    }
}
