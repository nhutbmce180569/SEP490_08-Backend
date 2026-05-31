namespace AIAPI.Services;

using AIAPI.DTOs;

public interface IInterRaterAgreementService
{
    InterRaterAgreementDTO ComputeAgreement();
}

public interface IPaperExportService
{
    PaperMethodologyDTO GetMethodology();
    Task<PaperBundleDTO> GeneratePaperBundleAsync(CancellationToken cancellationToken = default);
}

public interface IUserStudyPilotSeeder
{
    Task<SeedPilotStudyResponseDTO> SeedPilotParticipantsAsync(
        SeedPilotStudyRequestDTO request,
        CancellationToken cancellationToken = default);
}
