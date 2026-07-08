using AIAPI.DTOs;
using AIAPI.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;

namespace AIAPI.Controllers;

[Route("api/ai/intelligent-chat")]
[ApiController]
public class IntelligentChatController : LocalizedControllerBase
{
    private readonly IIntelligentChatService _chatService;
    private readonly IValidator<IntelligentChatRequestDTO> _chatValidator;

    public IntelligentChatController(
        IIntelligentChatService chatService,
        IValidator<IntelligentChatRequestDTO> chatValidator,
        IStringLocalizer<Messages> localizer)
        : base(localizer)
    {
        _chatService = chatService;
        _chatValidator = chatValidator;
    }

    [AllowAnonymous]
    [HttpPost("chat")]
    public async Task<ActionResult<IntelligentChatResponseDTO>> Chat(
        [FromBody] IntelligentChatRequestDTO request,
        CancellationToken cancellationToken)
    {
        var validation = await _chatValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)) });
        }

        try
        {
            var result = await _chatService.ChatAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
