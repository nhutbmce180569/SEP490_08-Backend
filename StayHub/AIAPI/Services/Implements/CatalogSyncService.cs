using AIAPI.Clients;
using AIAPI.DTOs;
using AIAPI.ML;
using AIAPI.Models;
using AIAPI.Recommender;
using AIAPI.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AIAPI.Services.Implements;

public class CatalogSyncService : ICatalogSyncService
{
    private readonly IGatewayCatalogClient _gatewayClient;
    private readonly ICatalogStore _catalogStore;
    private readonly IMlModelRegistry _modelRegistry;
    private readonly RecommenderSettings _recommenderSettings;
    private readonly ILogger<CatalogSyncService> _logger;

    public CatalogSyncService(
        IGatewayCatalogClient gatewayClient,
        ICatalogStore catalogStore,
        IMlModelRegistry modelRegistry,
        IOptions<RecommenderSettings> recommenderSettings,
        ILogger<CatalogSyncService> logger)
    {
        _gatewayClient = gatewayClient;
        _catalogStore = catalogStore;
        _modelRegistry = modelRegistry;
        _recommenderSettings = recommenderSettings.Value;
        _logger = logger;
    }

    public async Task SyncCatalogAsync(CancellationToken cancellationToken = default)
    {
        var baseTours = (await _gatewayClient.FetchActiveToursAsync(cancellationToken)).ToList();
        var tourism = await _gatewayClient.FetchActiveTourismAsync(cancellationToken);
        var categories = await _gatewayClient.FetchActiveCategoriesAsync(cancellationToken);

        CatalogAugmentationResult? augmentation = null;
        var catalogTours = baseTours;

        var aug = _recommenderSettings.CatalogAugmentation;
        if (aug.Enabled && baseTours.Count > 0)
        {
            augmentation = TourCatalogAugmentor.Augment(
                baseTours,
                targetSize: aug.TargetCatalogSize,
                maxVariantsPerBase: aug.MaxVariantsPerBase,
                jaccardThreshold: aug.JaccardThreshold,
                priceNoiseSigmaRatio: aug.PriceNoiseSigmaRatio,
                departureShiftDaysMax: aug.DepartureShiftDaysMax);

            catalogTours = augmentation.Catalog.ToList();
        }

        var stats = new CatalogStoreStats
        {
            BaseTourCount = augmentation?.BaseTourCount ?? baseTours.Count,
            AugmentedTourCount = augmentation?.VariantCount ?? 0,
            TotalTourCount = catalogTours.Count,
            RejectedByJaccard = augmentation?.RejectedByJaccard ?? 0,
            AvgPairwiseJaccardSample = augmentation?.AvgPairwiseJaccard ?? 0,
            AugmentationEnabled = aug.Enabled
        };

        _catalogStore.Update(catalogTours, tourism, categories, stats);

        if (_modelRegistry.Status.IsReady)
        {
            _modelRegistry.AttachCatalogVectors(catalogTours, tourism);
        }

        _logger.LogInformation(
            "Synced catalog: {BaseCount} base tours → {TotalCount} total ({VariantCount} variants, {Rejected} Jaccard rejects), {TourismCount} tourism.",
            stats.BaseTourCount,
            stats.TotalTourCount,
            stats.AugmentedTourCount,
            stats.RejectedByJaccard,
            tourism.Count);
    }
}

public class ModelTrainingService : IModelTrainingService
{
    private readonly ICatalogStore _catalogStore;
    private readonly ICatalogSyncService _catalogSyncService;
    private readonly IMlModelRegistry _modelRegistry;
    private readonly StayHubAiDbContext _dbContext;

    public ModelTrainingService(
        ICatalogStore catalogStore,
        ICatalogSyncService catalogSyncService,
        IMlModelRegistry modelRegistry,
        StayHubAiDbContext dbContext)
    {
        _catalogStore = catalogStore;
        _catalogSyncService = catalogSyncService;
        _modelRegistry = modelRegistry;
        _dbContext = dbContext;
    }

    public async Task<TrainedModelBundle> RetrainAsync(CancellationToken cancellationToken = default)
    {
        await _catalogSyncService.SyncCatalogAsync(cancellationToken);

        if (!_catalogStore.IsReady)
        {
            throw new InvalidOperationException("Catalog is empty. Ensure TourAPI has active tours before training.");
        }

        var run = new ModelTrainingRun
        {
            ModelName = "tour_assistant_bundle",
            Status = "Running",
            TourCount = _catalogStore.Tours.Count,
            TourismCount = _catalogStore.TourismItems.Count,
            StartedAt = DateTime.UtcNow
        };

        _dbContext.ModelTrainingRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var bundle = await _modelRegistry.TrainAndLoadAsync(
                _catalogStore.Tours,
                _catalogStore.TourismItems,
                cancellationToken);

            run.Status = "Completed";
            run.IntentAccuracy = bundle.IntentAccuracy;
            run.Message = "Models trained successfully with ML.NET.";
            run.CompletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return bundle;
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.Message = ex.Message;
            run.CompletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ModelTrainingStatusDTO> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var runs = await _dbContext.ModelTrainingRuns
            .OrderByDescending(r => r.StartedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        return new ModelTrainingStatusDTO
        {
            IsReady = _modelRegistry.Status.IsReady && _catalogStore.IsReady,
            LastTrainedAt = _modelRegistry.Status.TrainedAt,
            TourCatalogCount = _catalogStore.Tours.Count,
            TourismKnowledgeCount = _catalogStore.TourismItems.Count,
            ProfileMatchReady = _modelRegistry.Status.ProfileMatchReady,
            ProfileMatchTrainingSamples = _modelRegistry.Status.ProfileMatchTrainingSamples,
            IntentModelAccuracy = _modelRegistry.Status.IntentAccuracy,
            RecentRuns = runs.Select(r => new ModelTrainingRunDTO
            {
                Id = r.Id,
                ModelName = r.ModelName,
                Status = r.Status,
                TourCount = r.TourCount,
                TourismCount = r.TourismCount,
                IntentAccuracy = r.IntentAccuracy,
                Message = r.Message,
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt
            }).ToList()
        };
    }
}
