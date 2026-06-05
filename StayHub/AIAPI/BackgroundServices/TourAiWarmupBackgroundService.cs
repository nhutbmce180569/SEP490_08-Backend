using AIAPI.ML;
using AIAPI.Recommender;
using AIAPI.Services;
using AIAPI.Settings;
using Microsoft.Extensions.Options;

namespace AIAPI.BackgroundServices;

public class TourAiWarmupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MlSettings _settings;
    private readonly RecommenderSettings _recommenderSettings;
    private readonly ILogger<TourAiWarmupBackgroundService> _logger;

    public TourAiWarmupBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<MlSettings> settings,
        IOptions<RecommenderSettings> recommenderSettings,
        ILogger<TourAiWarmupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _recommenderSettings = recommenderSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        using var scope = _scopeFactory.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IMlModelRegistry>();
        var catalogSync = scope.ServiceProvider.GetRequiredService<ICatalogSyncService>();
        var training = scope.ServiceProvider.GetRequiredService<IModelTrainingService>();

        var modelsLoaded = registry.LoadFromDiskIfExists();
        if (!modelsLoaded)
        {
            _logger.LogInformation("ML models not loaded from disk. Will sync catalog and train if needed.");
        }

        var ragIndex = scope.ServiceProvider.GetRequiredService<IRagKnowledgeIndex>();
        ragIndex.Initialize(_recommenderSettings.RagCorpusMaxChunks);

        var systemKnowledge = scope.ServiceProvider.GetRequiredService<ISystemKnowledgeIndex>();
        systemKnowledge.Initialize();

        try
        {
            await catalogSync.SyncCatalogAsync(stoppingToken);

            if (_settings.RetrainOnStartupIfMissing || !registry.Status.IsReady)
            {
                _logger.LogInformation("Training ML.NET tour assistant models...");
                await training.RetrainAsync(stoppingToken);
                _logger.LogInformation("ML.NET tour assistant models are ready.");
            }
            else
            {
                var catalogStore = scope.ServiceProvider.GetRequiredService<ICatalogStore>();
                registry.AttachCatalogVectors(catalogStore.Tours, catalogStore.TourismItems);
                _logger.LogInformation("Loaded ML models from disk and attached catalog vectors.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Initial AI warmup failed. Service will retry on next sync interval.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(_settings.CatalogSyncIntervalMinutes), stoppingToken);

            try
            {
                using var loopScope = _scopeFactory.CreateScope();
                var sync = loopScope.ServiceProvider.GetRequiredService<ICatalogSyncService>();
                await sync.SyncCatalogAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Periodic catalog sync failed.");
            }
        }
    }
}
