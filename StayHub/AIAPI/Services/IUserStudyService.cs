using AIAPI.DTOs;

namespace AIAPI.Services;

public interface IUserStudyService
{
    UserStudyProtocolDTO GetProtocol();
    IReadOnlyList<UserStudyScenarioDTO> GetScenarios();
    Task<UserStudyComparisonDTO> GetComparisonAsync(string sessionId, int scenarioId, CancellationToken cancellationToken = default);
    Task<SubmitUserStudyResponseResultDTO> SubmitResponseAsync(SubmitUserStudyResponseDTO request, CancellationToken cancellationToken = default);
    Task<UserStudySummaryDTO> GetSummaryAsync(CancellationToken cancellationToken = default);
}
