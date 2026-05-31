using AIAPI.DTOs;
using AIAPI.Models.Catalog;

namespace AIAPI.Services;

public class GroundTruthBundle
{
    public string Mode { get; set; } = "";
    public string ProfileSignature { get; set; } = "";
    public string ProfileQueryKey { get; set; } = "";
    public Dictionary<int, int> Labels { get; set; } = new();
    public int ExpertJudgmentCount { get; set; }
    public int InteractionSignalCount { get; set; }
    public int ProxyRelevantCount { get; set; }
}

public interface IGroundTruthLabelService
{
    Task<GroundTruthBundle> BuildLabelsAsync(
        TourPreferenceQuestionnaireDTO profile,
        IReadOnlyList<TourCatalogItem> catalog,
        string mode,
        CancellationToken cancellationToken = default);

    Task<int> ImportJudgmentsAsync(
        IEnumerable<ExpertJudgmentInputDTO> judgments,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpertJudgmentDTO>> GetJudgmentsAsync(
        string? profileSignature,
        CancellationToken cancellationToken = default);
}
