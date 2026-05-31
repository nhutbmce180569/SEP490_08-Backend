using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.Models.Knowledge;
using AIAPI.ML;
using AIAPI.Recommender;
using Microsoft.ML;

namespace AIAPI.Services.Implements;

public class RagKnowledgeIndex : IRagKnowledgeIndex
{
    private readonly TourMlModelTrainer _trainer = new();
    private readonly object _lock = new();
    private ITransformer? _searchModel;
    private List<(RagCorpusChunk Chunk, float[] Vector)> _indexed = new();
    private RagCorpusBundle _corpus = new();

    public bool IsReady { get; private set; }
    public RagCorpusBundle Corpus => _corpus;

    public void Initialize()
    {
        lock (_lock)
        {
            if (IsReady)
            {
                return;
            }

            _corpus = LoadCorpus();
            if (_corpus.Chunks.Count == 0)
            {
                return;
            }

            _searchModel = _trainer.TrainTextSearchModel(
                _corpus.Chunks.Select(c => new TourDocument { Text = c.BuildSearchDocument() }));

            _indexed = _corpus.Chunks
                .Select(c => (c, _trainer.GetFeatureVector(_searchModel, c.BuildSearchDocument())))
                .ToList();

            IsReady = true;
        }
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
            if (!IsReady || _searchModel == null)
            {
                return Array.Empty<RagRetrievalResult>();
            }

            var interestList = interests?.ToList() ?? new List<string>();
            var enrichedQuery = string.Join(" ",
                new[] { query, city, string.Join(" ", interestList) }.Where(s => !string.IsNullOrWhiteSpace(s)));

            var queryVector = _trainer.GetFeatureVector(_searchModel, enrichedQuery);

            return _indexed
                .Select(item =>
                {
                    var semantic = _trainer.CosineSimilarity(queryVector, item.Vector);
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
            profile.HasElderly,
            profile.HasChildren,
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
