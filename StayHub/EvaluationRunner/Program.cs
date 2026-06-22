using System.Text.Json;
using System.Text.Json.Serialization;
using AIAPI.Clients;
using AIAPI.DTOs;
using AIAPI.Localization;
using AIAPI.ML;
using AIAPI.Models;
using AIAPI.Models.Catalog;
using AIAPI.Recommender;
using AIAPI.Services;
using AIAPI.Services.Implements;
using AIAPI.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EvaluationRunner;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static async Task<int> Main(string[] args)
    {
        var outputPath = args.Length > 0
            ? args[0]
            : Path.Combine(
                Environment.GetEnvironmentVariable("PAPER_RESULTS_PATH")
                ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Downloads/Overleaf/evaluation")),
                "paper_results.json");

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(AppContext.BaseDirectory);
                cfg.AddJsonFile("appsettings.json", optional: false);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.Configure<MlSettings>(ctx.Configuration.GetSection(MlSettings.SectionName));
                services.Configure<RecommenderSettings>(ctx.Configuration.GetSection(RecommenderSettings.SectionName));

                services.AddDbContext<StayHubAiDbContext>(options =>
                    options.UseInMemoryDatabase("StayHubEvalDb"));

                services.AddSingleton<ICatalogStore, CatalogStore>();
                services.AddSingleton<IRagKnowledgeIndex, RagKnowledgeIndex>();
                services.AddSingleton<IDimensionWeightProvider, DimensionWeightProvider>();
                services.AddSingleton<IAiCultureAccessor, EvalCultureAccessor>();
                services.AddScoped<IAiLocalizedCopy, AiLocalizedCopy>();
                services.AddScoped<TourScoringEngine>();
                services.AddScoped<DimensionWeightCalibrator>();
                services.AddScoped<TourRanker>();
                services.AddSingleton<IMlModelRegistry, MlModelRegistry>();
                services.AddSingleton<IEvaluationResultsExporter, EvaluationResultsExporter>();

                services.AddSingleton<IGatewayCatalogClient, FileCatalogClient>();
                services.AddScoped<ICatalogSyncService, CatalogSyncService>();
                services.AddScoped<IModelTrainingService, ModelTrainingService>();
                services.AddScoped<IRecommenderEvaluationService, RecommenderEvaluationService>();
                services.AddScoped<IGroundTruthLabelService, GroundTruthLabelService>();
            })
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning))
            .Build();

        using var scope = host.Services.CreateScope();
        var sp = scope.ServiceProvider;

        await SeedDatabaseAsync(sp);
        await WarmupAsync(sp);

        var evaluation = sp.GetRequiredService<IRecommenderEvaluationService>();
        var catalogStore = sp.GetRequiredService<ICatalogStore>();

        await evaluation.CalibrateDimensionWeightsAsync();

        var hybridAugmented = await evaluation.RunOfflineEvaluationAsync(new EvaluationRunRequestDTO
        {
            ProfileCount = EvaluationDataSpec.TestProfileCount,
            TopK = 8,
            RandomSeed = EvaluationDataSpec.DefaultRandomSeed,
            ProfileSplit = "test",
            LabelingMode = GroundTruthModes.Hybrid,
            IncludeAlphaSweep = true,
            IncludeSignificanceTests = true
        });

        var realOnly = await RunRealOnlyEvaluationAsync(sp, hybridAugmented);

        var bundle = new PaperResultsBundle
        {
            ExecutedAt = DateTime.UtcNow,
            ProtocolVersion = EvaluationDataSpec.ProtocolVersion,
            AugmentedCatalog = Summarize(hybridAugmented, catalogStore.Stats),
            RealOnlyCatalog = realOnly,
            SignificanceTests = hybridAugmented.SignificanceTests ?? [],
            AlphaSweep = hybridAugmented.AlphaSweepResults ?? []
        };

        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(bundle, JsonOptions));
        Console.WriteLine($"Wrote paper results -> {Path.GetFullPath(outputPath)}");
        return 0;
    }

    private static async Task SeedDatabaseAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<StayHubAiDbContext>();
        await db.Database.EnsureCreatedAsync();

        var seedPath = Path.Combine(AppContext.BaseDirectory, "Data", "evaluation-seed.json");
        if (!File.Exists(seedPath))
        {
            throw new FileNotFoundException("Missing evaluation seed JSON. Run scripts/export_evaluation_seed.py first.", seedPath);
        }

        var seed = JsonSerializer.Deserialize<EvaluationSeedFile>(
            await File.ReadAllTextAsync(seedPath), JsonOptions)
            ?? throw new InvalidOperationException("Invalid seed file.");

        foreach (var row in seed.Judgments)
        {
            db.TourRelevanceJudgments.Add(new TourRelevanceJudgment
            {
                ProfileSignature = row.ProfileSignature,
                ProfileQueryKey = row.ProfileQueryKey,
                TourId = row.TourId,
                RelevanceGrade = row.RelevanceGrade,
                Source = row.Source ?? "expert",
                JudgeId = row.JudgeId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task WarmupAsync(IServiceProvider sp)
    {
        var ragIndex = sp.GetRequiredService<IRagKnowledgeIndex>();
        var recommender = sp.GetRequiredService<IOptions<RecommenderSettings>>().Value;
        ragIndex.Initialize(recommender.RagCorpusMaxChunks);

        var catalogSync = sp.GetRequiredService<ICatalogSyncService>();
        await catalogSync.SyncCatalogAsync();

        var registry = sp.GetRequiredService<IMlModelRegistry>();
        var catalogStore = sp.GetRequiredService<ICatalogStore>();
        var training = sp.GetRequiredService<IModelTrainingService>();
        await training.RetrainAsync();
        registry.AttachCatalogVectors(catalogStore.Tours, catalogStore.TourismItems);
    }

    private static async Task<CatalogEvaluationSummary> RunRealOnlyEvaluationAsync(
        IServiceProvider sp,
        EvaluationRunResponseDTO augmentedRun)
    {
        var catalogStore = sp.GetRequiredService<ICatalogStore>();
        var baseTours = catalogStore.Tours.Where(t => t.Id < 10_000).ToList();
        var originalTours = catalogStore.Tours.ToList();
        var originalStats = catalogStore.Stats;

        catalogStore.Update(baseTours, catalogStore.TourismItems, new CatalogStoreStats
        {
            BaseTourCount = baseTours.Count,
            AugmentedTourCount = 0,
            TotalTourCount = baseTours.Count,
            AugmentationEnabled = false
        });

        var registry = sp.GetRequiredService<IMlModelRegistry>();
        registry.AttachCatalogVectors(baseTours, catalogStore.TourismItems);

        var evaluation = sp.GetRequiredService<IRecommenderEvaluationService>();
        var response = await evaluation.RunOfflineEvaluationAsync(new EvaluationRunRequestDTO
        {
            ProfileCount = EvaluationDataSpec.TestProfileCount,
            TopK = 8,
            RandomSeed = EvaluationDataSpec.DefaultRandomSeed,
            ProfileSplit = "test",
            LabelingMode = GroundTruthModes.Hybrid,
            IncludeAlphaSweep = false,
            IncludeSignificanceTests = false
        });

        catalogStore.Update(originalTours, catalogStore.TourismItems, originalStats!);
        registry.AttachCatalogVectors(originalTours, catalogStore.TourismItems);

        return Summarize(response, new CatalogStoreStats
        {
            BaseTourCount = baseTours.Count,
            TotalTourCount = baseTours.Count,
            AugmentationEnabled = false
        });
    }

    private static CatalogEvaluationSummary Summarize(
        EvaluationRunResponseDTO response,
        CatalogStoreStats? stats) =>
        new()
        {
            CatalogTourCount = response.CatalogTourCount,
            BaseTourCount = stats?.BaseTourCount ?? response.CatalogTourCount,
            ProfileCount = response.ProfileCount,
            Baselines = response.BaselineResults
                .Select(b => new BaselineSummary
                {
                    Key = b.StrategyKey,
                    Name = b.StrategyName,
                    NdcgAt8 = b.NdcgAtK,
                    NdcgStdDev = b.NdcgStdDev,
                    MinPersona = b.AvgMinPersonaUtility,
                    MinPersonaStdDev = b.MinPersonaStdDev,
                    DissatisfactionVariance = b.AvgDissatisfactionVariance,
                    DissatisfactionVarianceStdDev = b.DissatisfactionVarianceStdDev,
                    EnvyGap = b.AvgEnvyGap,
                    EnvyGapStdDev = b.EnvyGapStdDev
                })
                .OrderByDescending(b => b.NdcgAt8)
                .ToList()
        };
}

internal sealed class EvalCultureAccessor : IAiCultureAccessor
{
    public string Culture => "en";
    public bool IsVietnamese => false;
}

internal sealed class EvaluationSeedFile
{
    public List<SeedJudgment> Judgments { get; set; } = [];
    public List<SeedInteraction> Interactions { get; set; } = [];
}

internal sealed class SeedJudgment
{
    public string ProfileSignature { get; set; } = "";
    public string ProfileQueryKey { get; set; } = "";
    public int TourId { get; set; }
    public int RelevanceGrade { get; set; }
    public string? Source { get; set; }
    public string? JudgeId { get; set; }
}

internal sealed class SeedInteraction
{
    public int TourId { get; set; }
    public string InteractionType { get; set; } = "";
    public float Weight { get; set; }
    public string SessionId { get; set; } = "";
}

internal sealed class PaperResultsBundle
{
    public DateTime ExecutedAt { get; set; }
    public string ProtocolVersion { get; set; } = "";
    public CatalogEvaluationSummary AugmentedCatalog { get; set; } = new();
    public CatalogEvaluationSummary RealOnlyCatalog { get; set; } = new();
    public List<SignificanceTestDTO> SignificanceTests { get; set; } = [];
    public List<AlphaSweepPointDTO> AlphaSweep { get; set; } = [];
}

internal sealed class CatalogEvaluationSummary
{
    public int CatalogTourCount { get; set; }
    public int BaseTourCount { get; set; }
    public int ProfileCount { get; set; }
    public List<BaselineSummary> Baselines { get; set; } = [];
}

internal sealed class BaselineSummary
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public float NdcgAt8 { get; set; }
    public float NdcgStdDev { get; set; }
    public float MinPersona { get; set; }
    public float MinPersonaStdDev { get; set; }
    public float DissatisfactionVariance { get; set; }
    public float DissatisfactionVarianceStdDev { get; set; }
    public float EnvyGap { get; set; }
    public float EnvyGapStdDev { get; set; }
}
