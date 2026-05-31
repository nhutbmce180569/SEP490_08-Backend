using AIAPI.ML;
using AIAPI.Services;
using AIAPI.Settings;
using Microsoft.Extensions.Options;

namespace AIAPI.BackgroundServices;

public class TourAiWarmupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MlSettings _settings;
    private readonly ILogger<TourAiWarmupBackgroundService> _logger;

    public TourAiWarmupBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<MlSettings> settings,
        ILogger<TourAiWarmupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        using var scope = _scopeFactory.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IMlModelRegistry>();
        var catalogSync = scope.ServiceProvider.GetRequiredService<ICatalogSyncService>();
        var training = scope.ServiceProvider.GetRequiredService<IModelTrainingService>();

        registry.LoadFromDiskIfExists();

        try
        {
            await catalogSync.SyncCatalogAsync(stoppingToken);

            if (_settings.RetrainOnStartupIfMissing || !registry.Status.IsReady)
            {
                _logger.LogInformation("Training ML.NET tour assistant models...");
                await training.RetrainAsync(stoppingToken);
            }
            else
            {
                var catalogStore = scope.ServiceProvider.GetRequiredService<ICatalogStore>();
                registry.AttachCatalogVectors(catalogStore.Tours, catalogStore.TourismItems);
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
