using System.Security.Claims;
using AIAPI.DTOs;
using AIAPI.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;

namespace AIAPI.Controllers;

[Route("api/ai/tour-assistant")]
[ApiController]
public class TourAssistantController : LocalizedControllerBase
{
    private readonly ITourAssistantService _assistantService;
    private readonly ITourSemanticSearchService _searchService;
    private readonly ITourRecommendationService _recommendationService;
    private readonly IValidator<ChatRequestDTO> _chatValidator;
    private readonly IValidator<NaturalLanguageSearchRequestDTO> _searchValidator;
    private readonly IValidator<TourConsultationRequestDTO> _consultValidator;
    private readonly IValidator<LogInteractionRequestDTO> _interactionValidator;

    private readonly IPersonalizedTourRecommendationService _personalizedService;
    private readonly IValidator<TourPreferenceQuestionnaireDTO> _profileValidator;

    public TourAssistantController(
        ITourAssistantService assistantService,
        ITourSemanticSearchService searchService,
        ITourRecommendationService recommendationService,
        IPersonalizedTourRecommendationService personalizedService,
        IValidator<ChatRequestDTO> chatValidator,
        IValidator<NaturalLanguageSearchRequestDTO> searchValidator,
        IValidator<TourConsultationRequestDTO> consultValidator,
        IValidator<LogInteractionRequestDTO> interactionValidator,
        IValidator<TourPreferenceQuestionnaireDTO> profileValidator, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_assistantService = assistantService;
        _searchService = searchService;
        _recommendationService = recommendationService;
        _personalizedService = personalizedService;
        _chatValidator = chatValidator;
        _searchValidator = searchValidator;
        _consultValidator = consultValidator;
        _interactionValidator = interactionValidator;
        _profileValidator = profileValidator;
    }

    [AllowAnonymous]
    [HttpGet("scoring-model")]
    public ActionResult<ScoringModelDocumentationDTO> GetScoringModel()
    {
        return Ok(_personalizedService.GetScoringDocumentation());
    }

    [AllowAnonymous]
    [HttpGet("questionnaire")]
    public ActionResult<StandardQuestionnaireDTO> GetQuestionnaire()
    {
        return Ok(_personalizedService.GetStandardQuestionnaire());
    }

    [AllowAnonymous]
    [HttpPost("recommend-from-profile")]
    public async Task<ActionResult<PersonalizedRecommendationResponseDTO>> RecommendFromProfile(
        [FromBody] TourPreferenceQuestionnaireDTO request,
        CancellationToken cancellationToken)
    {
        var validation = await _profileValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var result = await _personalizedService.RecommendFromProfileAsync(request, GetOptionalCustomerId(), cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponseDTO>> Chat([FromBody] ChatRequestDTO request, CancellationToken cancellationToken)
    {
        var validation = await _chatValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var result = await _assistantService.ChatAsync(request.Message, request.SessionId, GetOptionalCustomerId(), cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("search")]
    public async Task<ActionResult<List<TourSearchResultItemDTO>>> Search(
        [FromBody] NaturalLanguageSearchRequestDTO request,
        CancellationToken cancellationToken)
    {
        var validation = await _searchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var result = await _searchService.SearchAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("recommend")]
    public async Task<ActionResult<List<TourRecommendationItemDTO>>> Recommend(
        [FromQuery] int top = 8,
        CancellationToken cancellationToken = default)
    {
        if (top <= 0 || top > 30)
        {
            return BadRequest(new { message = M("TopMustBeBetween1And30") });
        }

        try
        {
            var customerId = GetRequiredCustomerId();
            var result = await _recommendationService.RecommendAsync(customerId, top, null, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = M("LoginRequiredForPersonalizedRecommendations") });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("recommend/similar/{tourId:int}")]
    public async Task<ActionResult<List<TourRecommendationItemDTO>>> RecommendSimilar(
        int tourId,
        [FromQuery] int top = 6,
        CancellationToken cancellationToken = default)
    {
        if (top <= 0 || top > 20)
        {
            return BadRequest(new { message = M("TopMustBeBetween1And20") });
        }

        try
        {
            var result = await _recommendationService.RecommendSimilarAsync(tourId, top, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("consult")]
    public async Task<ActionResult<List<TourRecommendationItemDTO>>> Consult(
        [FromBody] TourConsultationRequestDTO request,
        CancellationToken cancellationToken)
    {
        var validation = await _consultValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var result = await _assistantService.ConsultAsync(request, GetOptionalCustomerId(), cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("tourism")]
    public async Task<ActionResult<List<TourismInsightDTO>>> GetTourismInsights(
        [FromQuery] string query,
        [FromQuery] string? city = null,
        [FromQuery] int top = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return BadRequest(new { message = M("QueryMustBeAtLeast2Characters") });
        }

        try
        {
            var result = await _searchService.SearchTourismAsync(query, city, top, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("interactions")]
    public async Task<IActionResult> LogInteraction([FromBody] LogInteractionRequestDTO request, CancellationToken cancellationToken)
    {
        var validation = await _interactionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            await _assistantService.LogInteractionAsync(GetRequiredCustomerId(), request, cancellationToken);
            return Ok(new { message = M("InteractionLoggedForMLRetrainingSignals") });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("health")]
    public ActionResult Health()
    {
        return Ok(new { status = "ok", service = "StayHub Tour AI Assistant (ML.NET)" });
    }

    private int? GetOptionalCustomerId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private int GetRequiredCustomerId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
        {
            throw new UnauthorizedAccessException("Cannot extract user ID from token.");
        }

        return id;
    }
}
