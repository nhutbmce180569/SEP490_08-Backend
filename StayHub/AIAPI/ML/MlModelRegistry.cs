using AIAPI.Models.Catalog;
using AIAPI.Settings;
using Microsoft.Extensions.Options;
using Microsoft.ML;

namespace AIAPI.ML;

public interface IMlModelRegistry
{
    TrainedModelBundle Status { get; }
    (string Intent, float Confidence) PredictIntent(string text);
    IReadOnlyList<(int TourId, float Score)> SearchTours(string query, int top, Func<TourCatalogItem, bool>? filter = null);
    IReadOnlyDictionary<int, float> ComputeTourSemanticScores(string query);
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
    private readonly object _lock = new();
    private readonly MLContext _mlContext = new(seed: 42);

    private ITransformer? _intentModel;
    private PredictionEngine<IntentExample, IntentPrediction>? _intentEngine;
    private ITransformer? _tourSearchModel;
    private ITransformer? _tourismSearchModel;
    private Dictionary<int, float[]> _tourVectors = new();
    private Dictionary<int, float[]> _tourismVectors = new();

    public MlModelRegistry(IOptions<MlSettings> settings, ILogger<MlModelRegistry> logger)
    {
        _settings = settings.Value;
        _logger = logger;
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
            if (_tourSearchModel == null || _tourVectors.Count == 0)
            {
                return Array.Empty<(int, float)>();
            }

            var queryVector = _trainer.GetFeatureVector(_tourSearchModel, query);
            return _tourVectors
                .Select(kv => (TourId: kv.Key, Score: _trainer.CosineSimilarity(queryVector, kv.Value)))
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
            if (_tourSearchModel == null || _tourVectors.Count == 0 || string.IsNullOrWhiteSpace(query))
            {
                return new Dictionary<int, float>();
            }

            var queryVector = _trainer.GetFeatureVector(_tourSearchModel, query);
            return _tourVectors.ToDictionary(
                kv => kv.Key,
                kv => _trainer.CosineSimilarity(queryVector, kv.Value));
        }
    }

    public IReadOnlyList<(int TourismId, float Score)> SearchTourism(string query, int top, string? city = null)
    {
        lock (_lock)
        {
            if (_tourismSearchModel == null || _tourismVectors.Count == 0)
            {
                return Array.Empty<(int, float)>();
            }

            var queryVector = _trainer.GetFeatureVector(_tourismSearchModel, query);
            return _tourismVectors
                .Select(kv => (TourismId: kv.Key, Score: _trainer.CosineSimilarity(queryVector, kv.Value)))
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
            var tourSearchModel = _trainer.TrainTextSearchModel(tourDocs);
            var tourVectors = tourDocs.ToDictionary(
                d => d.TourId,
                d => _trainer.GetFeatureVector(tourSearchModel, d.Text));

            var tourismDocs = tourismItems
                .Select(t => new TourismDocument { TourismId = t.Id, Text = t.SearchDocument })
                .ToList();

            ITransformer? tourismSearchModel = null;
            Dictionary<int, float[]> tourismVectors = new();
            if (tourismDocs.Count > 0)
            {
                tourismSearchModel = _trainer.TrainTourismSearchModel(tourismDocs);
                tourismVectors = tourismDocs.ToDictionary(
                    d => d.TourismId,
                    d => _trainer.GetFeatureVector(tourismSearchModel, d.Text));
            }

            lock (_lock)
            {
                _intentModel = intentModel;
                _intentEngine = _mlContext.Model.CreatePredictionEngine<IntentExample, IntentPrediction>(_intentModel);
                _tourSearchModel = tourSearchModel;
                _tourismSearchModel = tourismSearchModel;
                _tourVectors = tourVectors;
                _tourismVectors = tourismVectors;

                SaveModels(intentModel, tourSearchModel, tourismSearchModel);
            }

            Status = new TrainedModelBundle
            {
                IsReady = true,
                TrainedAt = DateTime.UtcNow,
                IntentAccuracy = accuracy,
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
        var tourPath = Path.Combine(dir, MlModelFiles.TourSearchModel);

        if (!IsValidModelFile(intentPath) || !IsValidModelFile(tourPath))
        {
            return false;
        }

        lock (_lock)
        {
            try
            {
                _intentModel = _mlContext.Model.Load(intentPath, out _);
                _intentEngine = _mlContext.Model.CreatePredictionEngine<IntentExample, IntentPrediction>(_intentModel);
                _tourSearchModel = _mlContext.Model.Load(tourPath, out _);

                var tourismPath = Path.Combine(dir, MlModelFiles.TourismSearchModel);
                if (IsValidModelFile(tourismPath))
                {
                    _tourismSearchModel = _mlContext.Model.Load(tourismPath, out _);
                }

                Status = new TrainedModelBundle
                {
                    IsReady = _tourSearchModel != null && _intentModel != null,
                    TrainedAt = File.GetLastWriteTimeUtc(tourPath)
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
            if (_tourSearchModel == null)
            {
                return;
            }

            _tourVectors = tours.ToDictionary(
                t => t.Id,
                t => _trainer.GetFeatureVector(_tourSearchModel, t.SearchDocument));

            if (_tourismSearchModel != null)
            {
                _tourismVectors = tourismItems.ToDictionary(
                    t => t.Id,
                    t => _trainer.GetFeatureVector(_tourismSearchModel, t.SearchDocument));
            }

            Status.TourCount = tours.Count;
            Status.TourismCount = tourismItems.Count;
            Status.IsReady = _tourVectors.Count > 0 && _intentEngine != null;
        }
    }

    private void SaveModels(ITransformer intentModel, ITransformer tourModel, ITransformer? tourismModel)
    {
        var dir = GetModelsDirectory();
        SaveModelAtomically(intentModel, Path.Combine(dir, MlModelFiles.IntentModel));
        SaveModelAtomically(tourModel, Path.Combine(dir, MlModelFiles.TourSearchModel));
        if (tourismModel != null)
        {
            SaveModelAtomically(tourismModel, Path.Combine(dir, MlModelFiles.TourismSearchModel));
        }
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
        _tourSearchModel = null;
        _tourismSearchModel = null;
        _tourVectors = new Dictionary<int, float[]>();
        _tourismVectors = new Dictionary<int, float[]>();
        Status = new TrainedModelBundle();
    }

    private void DeleteModelFiles(string dir)
    {
        foreach (var fileName in new[] { MlModelFiles.IntentModel, MlModelFiles.TourSearchModel, MlModelFiles.TourismSearchModel })
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
}
