using AIAPI.DTOs;
using AIAPI.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;

namespace AIAPI.Controllers;

[Route("api/ai/evaluation")]
[ApiController]
public class EvaluationController : LocalizedControllerBase
{
    private readonly IRecommenderEvaluationService _evaluationService;
    private readonly IGroundTruthLabelService _groundTruthService;
    private readonly IUserStudyService _userStudyService;
    private readonly IValidator<SubmitUserStudyResponseDTO> _userStudyValidator;
    private readonly IPaperExportService _paperExport;
    private readonly IUserStudyPilotSeeder _pilotSeeder;
    private readonly IInterRaterAgreementService _interRater;

    public EvaluationController(
        IRecommenderEvaluationService evaluationService,
        IGroundTruthLabelService groundTruthService,
        IUserStudyService userStudyService,
        IValidator<SubmitUserStudyResponseDTO> userStudyValidator,
        IPaperExportService paperExport,
        IUserStudyPilotSeeder pilotSeeder,
        IInterRaterAgreementService interRater,
        IStringLocalizer<Messages> localizer)
        : base(localizer)
    {
        _evaluationService = evaluationService;
        _groundTruthService = groundTruthService;
        _userStudyService = userStudyService;
        _userStudyValidator = userStudyValidator;
        _paperExport = paperExport;
        _pilotSeeder = pilotSeeder;
        _interRater = interRater;
    }

    [AllowAnonymous]
    [HttpGet("baselines")]
    public ActionResult<BaselineCatalogDTO> GetBaselines()
    {
        return Ok(_evaluationService.GetBaselineCatalog());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("calibrate-weights")]
    public async Task<ActionResult<WeightCalibrationResultDTO>> CalibrateWeights(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _evaluationService.CalibrateDimensionWeightsAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("rag-corpus-ablation")]
    public async Task<ActionResult<RagCorpusAblationResultDTO>> RagCorpusAblation(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _evaluationService.RunRagCorpusAblationAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("run")]
    public async Task<ActionResult<EvaluationRunResponseDTO>> RunEvaluation(
        [FromBody] EvaluationRunRequestDTO request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _evaluationService.RunOfflineEvaluationAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("judgments")]
    public async Task<ActionResult<ImportJudgmentsResponseDTO>> ImportJudgments(
        [FromBody] ImportJudgmentsRequestDTO request,
        CancellationToken cancellationToken)
    {
        var count = await _groundTruthService.ImportJudgmentsAsync(request.Judgments, cancellationToken);
        return Ok(new ImportJudgmentsResponseDTO { ImportedCount = count });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("judgments")]
    public async Task<ActionResult<IReadOnlyList<ExpertJudgmentDTO>>> GetJudgments(
        [FromQuery] string? profileSignature,
        CancellationToken cancellationToken)
    {
        var rows = await _groundTruthService.GetJudgmentsAsync(profileSignature, cancellationToken);
        return Ok(rows);
    }

    [AllowAnonymous]
    [HttpGet("user-study/protocol")]
    public ActionResult<UserStudyProtocolDTO> GetUserStudyProtocol()
    {
        return Ok(_userStudyService.GetProtocol());
    }

    [AllowAnonymous]
    [HttpGet("user-study/scenarios")]
    public ActionResult<IReadOnlyList<UserStudyScenarioDTO>> GetUserStudyScenarios()
    {
        return Ok(_userStudyService.GetScenarios());
    }

    [AllowAnonymous]
    [HttpGet("user-study/scenarios/{scenarioId:int}/comparison")]
    public async Task<ActionResult<UserStudyComparisonDTO>> GetUserStudyComparison(
        int scenarioId,
        [FromQuery] string sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userStudyService.GetComparisonAsync(sessionId, scenarioId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("user-study/responses")]
    public async Task<ActionResult<SubmitUserStudyResponseResultDTO>> SubmitUserStudyResponse(
        [FromBody] SubmitUserStudyResponseDTO request,
        CancellationToken cancellationToken)
    {
        var validation = await _userStudyValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var result = await _userStudyService.SubmitResponseAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("user-study/summary")]
    public async Task<ActionResult<UserStudySummaryDTO>> GetUserStudySummary(CancellationToken cancellationToken)
    {
        var summary = await _userStudyService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [AllowAnonymous]
    [HttpGet("methodology")]
    public ActionResult<PaperMethodologyDTO> GetPaperMethodology()
    {
        return Ok(_paperExport.GetMethodology());
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("paper-bundle")]
    public async Task<ActionResult<PaperBundleDTO>> GetPaperBundle(CancellationToken cancellationToken)
    {
        try
        {
            var bundle = await _paperExport.GeneratePaperBundleAsync(cancellationToken);
            return Ok(bundle);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("user-study/seed-pilot")]
    public async Task<ActionResult<SeedPilotStudyResponseDTO>> SeedPilotUserStudy(
        [FromBody] SeedPilotStudyRequestDTO request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _pilotSeeder.SeedPilotParticipantsAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("inter-rater-agreement")]
    public ActionResult<InterRaterAgreementDTO> GetInterRaterAgreement()
    {
        return Ok(_interRater.ComputeAgreement());
    }
}
