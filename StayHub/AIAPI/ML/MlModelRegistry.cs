using AIAPI.Models.Catalog;
using AIAPI.DTOs;
using AIAPI.Recommender;
using AIAPI.Settings;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using AIAPI.Services;

namespace AIAPI.ML;

public interface IMlModelRegistry
{
    TrainedModelBundle Status { get; }
    (string Intent, float Confidence) PredictIntent(string text);
    IReadOnlyList<(int TourId, float Score)> SearchTours(string query, int top, Func<TourCatalogItem, bool>? filter = null);
    IReadOnlyDictionary<int, float> ComputeTourSemanticScores(string query);
    IReadOnlyDictionary<int, float> ComputeProfileMatchScores(TourPreferenceQuestionnaireDTO profile);
    IReadOnlyList<(int TourismId, float Score)> SearchTourism(string query, int top, string? city = null);
    Task<TrainedModelBundle> TrainAndLoadAsync(
        IReadOnlyList<TourCatalogItem> tours,
        IReadOnlyList<TourismKnowledgeItem> tourismItems,
        CancellationToken cancellationToken = default);
    bool LoadFromDiskIfExists();
    void AttachCatalogVectors(IReadOnlyList<TourCatalogItem> tours, IReadOnlyList<TourismKnowledgeItem> tourismItems);
}

public class MlModelRegistry : IMlModelRegistry
{
    private readonly TourMlModelTrainer _trainer = new();
    private readonly MlSettings _settings;
    private readonly ILogger<MlModelRegistry> _logger;
    private readonly ILocalEmbeddingService _embeddingService;
    private readonly object _lock = new();
    private readonly MLContext _mlContext = new(seed: 42);

    private ITransformer? _intentModel;
    private PredictionEngine<IntentExample, IntentPrediction>? _intentEngine;
    private ITransformer? _profileMatchModel;
    private PredictionEngine<TourProfileMatchExample, TourProfileMatchPrediction>? _profileMatchEngine;
    private Dictionary<int, float[]> _tourVectors = new();
    private Dictionary<int, float[]> _tourismVectors = new();
    private Dictionary<int, TourCatalogItem> _toursData = new();
    private Dictionary<int, TourismKnowledgeItem> _tourismData = new();

    public MlModelRegistry(
        IOptions<MlSettings> settings, 
        ILogger<MlModelRegistry> logger,
        ILocalEmbeddingService embeddingService)
    {
        _settings = settings.Value;
        _logger = logger;
        _embeddingService = embeddingService;
        Directory.CreateDirectory(GetModelsDirectory());
    }

    public TrainedModelBundle Status { get; private set; } = new();

    public (string Intent, float Confidence) PredictIntent(string text)
    {
        lock (_lock)
        {
            if (_intentEngine == null)
            {
                return (TourIntents.Unknown, 0f);
            }

            var prediction = _intentEngine.Predict(new IntentExample { Text = text });
            var confidence = prediction.Score.Length > 0 ? prediction.Score.Max() : 0f;
            return (string.IsNullOrWhiteSpace(prediction.Label) ? TourIntents.Unknown : prediction.Label, confidence);
        }
    }

    public IReadOnlyList<(int TourId, float Score)> SearchTours(string query, int top, Func<TourCatalogItem, bool>? filter = null)
    {
        lock (_lock)
        {
            if (_tourVectors.Count == 0 || string.IsNullOrWhiteSpace(query))
            {
                return Array.Empty<(int, float)>();
            }

            var queryVector = _embeddingService.EmbedText(query);
            IEnumerable<KeyValuePair<int, float[]>> candidateVectors = _tourVectors;

            if (filter != null)
            {
                candidateVectors = candidateVectors.Where(kv => 
                    _toursData.TryGetValue(kv.Key, out var tour) && filter(tour));
            }

            return candidateVectors
                .Select(kv => (TourId: kv.Key, Score: _embeddingService.CosineSimilarity(queryVector, kv.Value)))
                .Where(x => x.Score >= _settings.MinSemanticScore)
                .OrderByDescending(x => x.Score)
                .Take(top)
                .ToList();
        }
    }

    public IReadOnlyDictionary<int, float> ComputeTourSemanticScores(string query)
    {
        lock (_lock)
        {
            if (_tourVectors.Count == 0 || string.IsNullOrWhiteSpace(query))
            {
                return new Dictionary<int, float>();
            }

            var queryVector = _embeddingService.EmbedText(query);
            return _tourVectors.ToDictionary(
                kv => kv.Key,
                kv => _embeddingService.CosineSimilarity(queryVector, kv.Value));
        }
    }

    public IReadOnlyDictionary<int, float> ComputeProfileMatchScores(TourPreferenceQuestionnaireDTO profile)
    {
        lock (_lock)
        {
            if (_profileMatchEngine == null || _toursData.Count == 0)
            {
                return new Dictionary<int, float>();
            }

            var profileBudget = profile.MaxBudgetPerPerson ?? 100000000f;
            var profileDuration = 3f;
            if (profile.PreferredEndDate.HasValue)
            {
                profileDuration = (float)(profile.PreferredEndDate.Value - profile.PreferredStartDate).TotalDays + 1f;
            }
            
            var interestQuery = string.Join(" ", profile.TravelInterests);
            var profileInterestVector = _embeddingService.EmbedText(interestQuery);

            var rawScores = _toursData.Values
                .Where(t => t.Id > 0 && t.Id < 100000)
                .Select(tour =>
                {
                    var tourVector = _tourVectors.GetValueOrDefault(tour.Id, Array.Empty<float>());
                    var interestMatch = tourVector.Length > 0 && profileInterestVector.Length > 0 ? _embeddingService.CosineSimilarity(profileInterestVector, tourVector) : 0f;
                    var cityMatch = !string.IsNullOrWhiteSpace(profile.PreferredCity) && 
                                   string.Equals(profile.PreferredCity, tour.City, StringComparison.OrdinalIgnoreCase) 
                                   ? 1f : 0f;

                    return new
                    {
                        TourId = tour.Id,
                        Score = _profileMatchEngine.Predict(new TourProfileMatchExample
                        {
                            TourPrice = (float)(tour.MinPrice ?? 0),
                            TourDuration = tour.DurationDays ?? 3f,
                            ProfileBudget = profileBudget,
                            ProfileDuration = profileDuration,
                            CityMatch = cityMatch,
                            InterestMatchScore = interestMatch,
                            AdventureLevel = ComputeAdventureLevel(tour),
                            AgeSuitability = ComputeAgeSuitability(tour, profile),
                            DifficultyScore = ComputeDifficultyScore(tour)
                        }).Score
                    };
                })
                .ToList();

            if (rawScores.Count == 0)
            {
                return new Dictionary<int, float>();
            }

            var min = rawScores.Min(x => x.Score);
            var max = rawScores.Max(x => x.Score);
            var range = max - min;

            return rawScores.ToDictionary(
                x => x.TourId,
                x => range <= 1e-6f ? 0.5f : Math.Clamp((x.Score - min) / range, 0f, 1f));
        }
    }

    public IReadOnlyList<(int TourismId, float Score)> SearchTourism(string query, int top, string? city = null)
    {
        lock (_lock)
        {
            if (_tourismVectors.Count == 0 || string.IsNullOrWhiteSpace(query))
            {
                return Array.Empty<(int, float)>();
            }

            var queryVector = _embeddingService.EmbedText(query);
            IEnumerable<KeyValuePair<int, float[]>> candidateVectors = _tourismVectors;

            if (!string.IsNullOrWhiteSpace(city))
            {
                candidateVectors = candidateVectors.Where(kv => 
                    _tourismData.TryGetValue(kv.Key, out var item) && 
                    AIAPI.Helpers.VietnameseTextNormalizer.CityEquals(item.City, city));
            }

            return candidateVectors
                .Select(kv => (TourismId: kv.Key, Score: _embeddingService.CosineSimilarity(queryVector, kv.Value)))
                .Where(x => x.Score >= _settings.MinSemanticScore)
                .OrderByDescending(x => x.Score)
                .Take(top)
                .ToList();
        }
    }

    public async Task<TrainedModelBundle> TrainAndLoadAsync(
        IReadOnlyList<TourCatalogItem> tours,
        IReadOnlyList<TourismKnowledgeItem> tourismItems,
        CancellationToken cancellationToken = default)
    {
        if (tours.Count == 0)
        {
            throw new InvalidOperationException("Cannot train models without active tours in catalog.");
        }

        return await Task.Run(() =>
        {
            var (intentModel, accuracy) = _trainer.TrainIntentModel(IntentTrainingData.GetSamples());

            var tourDocs = tours.Select(t => new TourDocument { TourId = t.Id, Text = t.SearchDocument }).ToList();
            var tourVectors = tours.ToDictionary(
                d => d.Id,
                d => d.SemanticEmbedding ?? _embeddingService.EmbedText(d.SearchDocument));

            Dictionary<int, float[]> tourismVectors = tourismItems.ToDictionary(
                d => d.Id,
                d => _embeddingService.EmbedText(d.SearchDocument));

            var pmExamples = BuildProfileMatchTrainingRows(tours, tourVectors);
            var pmModel = _trainer.TrainProfileMatchRegressionModel(pmExamples);
            var toursData = tours.ToDictionary(t => t.Id);
            var tourismData = tourismItems.ToDictionary(t => t.Id);

            lock (_lock)
            {
                _intentModel = intentModel;
                _intentEngine = _mlContext.Model.CreatePredictionEngine<IntentExample, IntentPrediction>(_intentModel);
                _profileMatchModel = pmModel;
                _profileMatchEngine = _mlContext.Model.CreatePredictionEngine<TourProfileMatchExample, TourProfileMatchPrediction>(_profileMatchModel);
                _tourVectors = tourVectors;
                _tourismVectors = tourismVectors;
                _toursData = toursData;
                _tourismData = tourismData;

                SaveModels(intentModel, pmModel);
            }

            Status = new TrainedModelBundle
            {
                IsReady = true,
                TrainedAt = DateTime.UtcNow,
                IntentAccuracy = accuracy,
                ProfileMatchReady = true,
                ProfileMatchTrainingSamples = pmExamples.Count,
                TourCount = tours.Count,
                TourismCount = tourismItems.Count
            };

            return Status;
        }, cancellationToken);
    }

    public bool LoadFromDiskIfExists()
    {
        var dir = GetModelsDirectory();
        var intentPath = Path.Combine(dir, MlModelFiles.IntentModel);
        var mfPath = Path.Combine(dir, MlModelFiles.TourProfileMatchRegressionModel);

        if (!IsValidModelFile(intentPath) || !IsValidModelFile(mfPath))
        {
            return false;
        }

        lock (_lock)
        {
            try
            {
                _intentModel = _mlContext.Model.Load(intentPath, out _);
                _intentEngine = _mlContext.Model.CreatePredictionEngine<IntentExample, IntentPrediction>(_intentModel);
                _profileMatchModel = _mlContext.Model.Load(mfPath, out _);
                _profileMatchEngine = _mlContext.Model.CreatePredictionEngine<TourProfileMatchExample, TourProfileMatchPrediction>(_profileMatchModel);

                Status = new TrainedModelBundle
                {
                    IsReady = _intentModel != null && _profileMatchModel != null,
                    TrainedAt = File.GetLastWriteTimeUtc(mfPath),
                    ProfileMatchReady = _profileMatchModel != null
                };

                return Status.IsReady;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load ML models from disk. Corrupt files will be removed and models retrained.");
                ResetInMemoryModels();
                DeleteModelFiles(dir);
                return false;
            }
        }
    }

    public void AttachCatalogVectors(IReadOnlyList<TourCatalogItem> tours, IReadOnlyList<TourismKnowledgeItem> tourismItems)
    {
        lock (_lock)
        {
            _tourVectors = tours.ToDictionary(
                t => t.Id,
                t => t.SemanticEmbedding ?? _embeddingService.EmbedText(t.SearchDocument));

            _tourismVectors = tourismItems.ToDictionary(
                t => t.Id,
                t => _embeddingService.EmbedText(t.SearchDocument));

            Status.TourCount = tours.Count;
            Status.TourismCount = tourismItems.Count;
            Status.ProfileMatchReady = _profileMatchEngine != null;
            _toursData = tours.ToDictionary(t => t.Id);
            _tourismData = tourismItems.ToDictionary(t => t.Id);
            Status.IsReady = _tourVectors.Count > 0 && _intentEngine != null && _profileMatchEngine != null;
        }
    }

    private void SaveModels(
        ITransformer intentModel,
        ITransformer profileMatchModel)
    {
        var dir = GetModelsDirectory();
        SaveModelAtomically(intentModel, Path.Combine(dir, MlModelFiles.IntentModel));
        SaveModelAtomically(profileMatchModel, Path.Combine(dir, MlModelFiles.TourProfileMatchRegressionModel));
    }

    private void SaveModelAtomically(ITransformer model, string destinationPath)
    {
        var tempPath = destinationPath + ".tmp";
        _mlContext.Model.Save(model, null, tempPath);
        File.Move(tempPath, destinationPath, overwrite: true);
    }

    private static bool IsValidModelFile(string path) =>
        File.Exists(path) && new FileInfo(path).Length > 0;

    private void ResetInMemoryModels()
    {
        _intentModel = null;
        _intentEngine = null;
        _profileMatchModel = null;
        _profileMatchEngine = null;
        _tourVectors = new Dictionary<int, float[]>();
        _tourismVectors = new Dictionary<int, float[]>();
        _toursData = new Dictionary<int, TourCatalogItem>();
        _tourismData = new Dictionary<int, TourismKnowledgeItem>();
        Status = new TrainedModelBundle();
    }

    private void DeleteModelFiles(string dir)
    {
        foreach (var fileName in new[] { MlModelFiles.IntentModel, MlModelFiles.TourProfileMatchRegressionModel })
        {
            var path = Path.Combine(dir, fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete corrupt model file {ModelFile}.", fileName);
            }
        }
    }

    private string GetModelsDirectory() =>
        Path.IsPathRooted(_settings.ModelsDirectory)
            ? _settings.ModelsDirectory
            : Path.Combine(AppContext.BaseDirectory, _settings.ModelsDirectory);

    private static List<TourProfileMatchExample> BuildProfileMatchTrainingRows(
        IReadOnlyList<TourCatalogItem> tours, 
        Dictionary<int, float[]> tourVectors)
    {
        var rows = new List<TourProfileMatchExample>();
        var random = new Random(42);

        foreach (var tour in tours.Where(t => t.Id > 0 && t.Id < 100000))
        {
            var popularity = ComputePopularityPrior(tour);
            var tourPrice = (float)(tour.MinPrice ?? 0);
            var tourDuration = tour.DurationDays ?? 3f;

            // Generate 10 synthetic profile variations per tour
            for (int i = 0; i < 10; i++)
            {
                // Positive case (High Match)
                var isPositive = i < 5;
                
                var profileBudget = isPositive ? tourPrice * (1f + (float)random.NextDouble()) : tourPrice * (float)random.NextDouble() * 0.8f;
                if (profileBudget < 100000) profileBudget = 100000000f; // No budget limit
                
                var profileDuration = isPositive ? tourDuration : (tourDuration > 3f ? 2f : 5f);
                var cityMatch = isPositive ? 1f : (random.NextDouble() > 0.8 ? 1f : 0f);
                var interestMatch = isPositive ? 0.7f + (float)random.NextDouble() * 0.3f : (float)random.NextDouble() * 0.4f;

                // Base label based on features
                var label = popularity * 0.2f 
                            + cityMatch * 0.3f 
                            + interestMatch * 0.5f;

                // Penalties
                if (tourPrice > profileBudget) label -= 0.4f;
                if (tourDuration > profileDuration) label -= 0.3f;

                float advLevel = isPositive ? 0.8f : (float)random.NextDouble();
                float ageSuitability = isPositive ? 0.9f : (float)random.NextDouble();
                float diffScore = isPositive ? 0.2f : (float)random.NextDouble() * 0.8f + 0.2f;

                rows.Add(new TourProfileMatchExample
                {
                    TourPrice = tourPrice,
                    TourDuration = tourDuration,
                    ProfileBudget = profileBudget,
                    ProfileDuration = profileDuration,
                    CityMatch = cityMatch,
                    InterestMatchScore = interestMatch,
                    AdventureLevel = advLevel,
                    AgeSuitability = ageSuitability,
                    DifficultyScore = diffScore,
                    Label = Math.Clamp(label, 0f, 1f)
                });
            }
        }

        return rows;
    }

    private static float ComputePopularityPrior(TourCatalogItem tour)
    {
        var rating = (float)((tour.AverageStar ?? 3.5) / 5.0);
        var review = Math.Min(tour.ReviewCount / 25f, 1f);
        return Math.Clamp(rating * 0.7f + review * 0.3f, 0f, 1f);
    }

    private static float ComputeAccessibilityBonus(TourCatalogItem tour)
    {
        var duration = tour.DurationDays ?? 3;
        return duration <= 4 ? 0.35f : 0f;
    }

    private static float ComputeArchetypeCityBonus(uint archetype, string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return 0f;
        }

        var c = city.ToLowerInvariant();
        if (archetype == TourRecommendationArchetypes.RiverCity &&
            (c.Contains("can tho") || c.Contains("cần thơ") || c.Contains("mekong")))
        {
            return 0.45f;
        }

        if (archetype == TourRecommendationArchetypes.BeachRelax &&
            (c.Contains("phu quoc") || c.Contains("phú quốc") || c.Contains("nha trang") || c.Contains("da nang") || c.Contains("đà nẵng")))
        {
            return 0.45f;
        }

        if (archetype == TourRecommendationArchetypes.CultureFood &&
            (c.Contains("hue") || c.Contains("huế") || c.Contains("hoi an") || c.Contains("hội an") || c.Contains("ha noi") || c.Contains("hà nội")))
        {
            return 0.45f;
        }

        return 0f;
    }

    private static float ComputeAdventureLevel(TourCatalogItem tour)
    {
        var doc = tour.SearchDocument.ToLowerInvariant();
        if (doc.Contains("trekking") || doc.Contains("leo núi") || doc.Contains("adventure")) return 0.9f;
        if (doc.Contains("khám phá") || doc.Contains("explore")) return 0.6f;
        return 0.2f;
    }

    private static float ComputeAgeSuitability(TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile)
    {
        if (profile.ElderlyCount > 0 && tour.DurationDays > 5) return 0.3f;
        if (profile.ChildrenCount > 0 && tour.SearchDocument.Contains("resort", StringComparison.OrdinalIgnoreCase)) return 0.9f;
        return 0.7f;
    }

    private static float ComputeDifficultyScore(TourCatalogItem tour)
    {
        return ComputeAdventureLevel(tour) * 0.8f + (tour.DurationDays ?? 3) * 0.05f;
    }
}
