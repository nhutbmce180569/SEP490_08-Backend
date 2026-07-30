using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.Models.Knowledge;
using AIAPI.ML;
using AIAPI.Recommender;
using Microsoft.ML;

namespace AIAPI.Services.Implements;

public class RagKnowledgeIndex : IRagKnowledgeIndex
{
    private readonly ILocalEmbeddingService _embeddingService;
    private readonly object _lock = new();
    private List<(RagCorpusChunk Chunk, float[] Vector)> _indexed = new();
    private RagCorpusBundle _corpus = new();

    public RagKnowledgeIndex(ILocalEmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    public bool IsReady { get; private set; }
    public int ActiveChunkCount { get; private set; }
    public RagCorpusBundle Corpus => _corpus;

    public void Initialize(int? maxChunks = null) => Reinitialize(maxChunks);

    public void Reinitialize(int? maxChunks = null)
    {
        lock (_lock)
        {
            IsReady = false;
            _indexed = new List<(RagCorpusChunk Chunk, float[] Vector)>();

            _corpus = LoadCorpus();
            var chunks = SelectChunks(_corpus.Chunks, maxChunks);
            if (chunks.Count == 0)
            {
                ActiveChunkCount = 0;
                return;
            }

            _indexed = chunks
                .Select(c => (c, _embeddingService.EmbedText(c.BuildSearchDocument())))
                .ToList();

            ActiveChunkCount = chunks.Count;
            IsReady = true;
        }
    }

    private static List<RagCorpusChunk> SelectChunks(IReadOnlyList<RagCorpusChunk> all, int? maxChunks)
    {
        if (maxChunks is null or <= 0 || maxChunks >= all.Count)
        {
            return all.ToList();
        }

        var perCity = all
            .GroupBy(c => c.CityKeys.FirstOrDefault() ?? c.Id)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        var selected = new List<RagCorpusChunk>();
        var target = maxChunks.Value;

        while (selected.Count < target)
        {
            var added = false;
            foreach (var group in perCity)
            {
                var remaining = group.Where(c => !selected.Contains(c)).ToList();
                if (remaining.Count == 0)
                {
                    continue;
                }

                selected.Add(remaining[0]);
                added = true;
                if (selected.Count >= target)
                {
                    break;
                }
            }

            if (!added)
            {
                break;
            }
        }

        return selected;
    }

    public IReadOnlyList<RagRetrievalResult> Retrieve(
        string query,
        string? city,
        IEnumerable<string>? interests,
        bool forForeignVisitor,
        bool forElderly,
        bool forChildren,
        int topK = 6)
    {
        lock (_lock)
        {
            if (!IsReady || _indexed.Count == 0)
            {
                return Array.Empty<RagRetrievalResult>();
            }

            var interestList = interests?.ToList() ?? new List<string>();
            var enrichedQuery = string.Join(" ",
                new[] { query, city, string.Join(" ", interestList) }.Where(s => !string.IsNullOrWhiteSpace(s)));

            var queryVector = _embeddingService.EmbedText(enrichedQuery);

            return _indexed
                .Select(item =>
                {
                    var semantic = _embeddingService.CosineSimilarity(queryVector, item.Vector);
                    var boost = ComputeBoost(item.Chunk, city, interestList, forForeignVisitor, forElderly, forChildren);
                    return new RagRetrievalResult { Chunk = item.Chunk, Score = semantic * 0.7f + boost * 0.3f };
                })
                .Where(r => r.Score > 0.05f)
                .OrderByDescending(r => r.Score)
                .Take(topK)
                .ToList();
        }
    }

    public float ScoreTourCulturalFit(int tourId, string? tourCity, TourPreferenceQuestionnaireDTO profile)
    {
        var query = string.Join(" ", profile.TravelInterests);
        var results = Retrieve(
            query,
            profile.PreferredCity ?? tourCity,
            profile.TravelInterests,
            profile.NationalityType == TravelerNationalityTypes.Foreigner,
            profile.ElderlyCount > 0,
            profile.ChildrenCount > 0,
            topK: 8);

        if (results.Count == 0)
        {
            return 0.5f;
        }

        var tourHits = results.Where(r =>
            r.Chunk.RelatedTourIds.Contains(tourId) ||
            (!string.IsNullOrWhiteSpace(tourCity) &&
             r.Chunk.CityKeys.Any(k => VietnameseTextNormalizer.CityEquals(tourCity, k)))).ToList();

        if (tourHits.Count == 0)
        {
            return 0.35f + results.Max(r => r.Score) * 0.2f;
        }

        var maxScore = tourHits.Max(r => r.Score);
        var unescoBoost = tourHits.Any(r => r.Chunk.HeritageLevel.Contains("UNESCO", StringComparison.OrdinalIgnoreCase))
            ? 0.1f
            : 0f;

        return Math.Clamp(maxScore + unescoBoost, 0f, 1f);
    }

    private static float ComputeBoost(
        RagCorpusChunk chunk,
        string? city,
        IReadOnlyList<string> interests,
        bool forForeignVisitor,
        bool forElderly,
        bool forChildren)
    {
        var boost = 0f;

        if (!string.IsNullOrWhiteSpace(city) &&
            chunk.CityKeys.Any(k => VietnameseTextNormalizer.CityEquals(city, k)))
        {
            boost += 0.35f;
        }

        if (interests.Count > 0)
        {
            var overlap = interests.Count(i =>
                chunk.InterestTags.Any(t => t.Equals(i, StringComparison.OrdinalIgnoreCase)));
            boost += overlap / (float)Math.Max(interests.Count, 1) * 0.35f;
        }

        if (forForeignVisitor && chunk.PersonaTags.Any(p => p.Equals("foreigner", StringComparison.OrdinalIgnoreCase)))
        {
            boost += 0.1f;
        }

        if (forElderly && chunk.ChunkType is "accessibility" or "heritage")
        {
            boost += 0.1f;
        }

        if (forChildren && chunk.PersonaTags.Any(p => p is "children" or "family"))
        {
            boost += 0.1f;
        }

        return Math.Min(boost, 1f);
    }

    private static RagCorpusBundle LoadCorpus()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "vietnam-rag-corpus.json");
        if (!File.Exists(path))
        {
            return new RagCorpusBundle();
        }

        var json = File.ReadAllText(path);
        return System.Text.Json.JsonSerializer.Deserialize<RagCorpusBundle>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new RagCorpusBundle();
    }
}
