using AIAPI.Clients;
using AIAPI.DTOs;
using AIAPI.ML;
using AIAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AIAPI.Services.Implements;

public class CatalogSyncService : ICatalogSyncService
{
    private readonly IGatewayCatalogClient _gatewayClient;
    private readonly ICatalogStore _catalogStore;
    private readonly IMlModelRegistry _modelRegistry;
    private readonly ILogger<CatalogSyncService> _logger;

    public CatalogSyncService(
        IGatewayCatalogClient gatewayClient,
        ICatalogStore catalogStore,
        IMlModelRegistry modelRegistry,
        ILogger<CatalogSyncService> logger)
    {
        _gatewayClient = gatewayClient;
        _catalogStore = catalogStore;
        _modelRegistry = modelRegistry;
        _logger = logger;
    }

    public async Task SyncCatalogAsync(CancellationToken cancellationToken = default)
    {
        var tours = await _gatewayClient.FetchActiveToursAsync(cancellationToken);
        var tourism = await _gatewayClient.FetchActiveTourismAsync(cancellationToken);
        _catalogStore.Update(tours, tourism);

        if (_modelRegistry.Status.IsReady)
        {
            _modelRegistry.AttachCatalogVectors(tours, tourism);
        }

        _logger.LogInformation("Synced {TourCount} tours and {TourismCount} tourism records.", tours.Count, tourism.Count);
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

        var interactionCount = await _dbContext.UserTourInteractions.CountAsync(cancellationToken);
        var run = new ModelTrainingRun
        {
            ModelName = "tour_assistant_bundle",
            Status = "Running",
            TourCount = _catalogStore.Tours.Count,
            TourismCount = _catalogStore.TourismItems.Count,
            InteractionCount = interactionCount,
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

        var interactionCount = await _dbContext.UserTourInteractions.CountAsync(cancellationToken);

        return new ModelTrainingStatusDTO
        {
            IsReady = _modelRegistry.Status.IsReady && _catalogStore.IsReady,
            LastTrainedAt = _modelRegistry.Status.TrainedAt,
            TourCatalogCount = _catalogStore.Tours.Count,
            TourismKnowledgeCount = _catalogStore.TourismItems.Count,
            InteractionCount = interactionCount,
            IntentModelAccuracy = _modelRegistry.Status.IntentAccuracy,
            RecentRuns = runs.Select(r => new ModelTrainingRunDTO
            {
                Id = r.Id,
                ModelName = r.ModelName,
                Status = r.Status,
                TourCount = r.TourCount,
                TourismCount = r.TourismCount,
                InteractionCount = r.InteractionCount,
                IntentAccuracy = r.IntentAccuracy,
                Message = r.Message,
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt
            }).ToList()
        };
    }
}
