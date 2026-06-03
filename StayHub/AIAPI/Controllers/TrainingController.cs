using AIAPI.DTOs;
using AIAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;

namespace AIAPI.Controllers;

[Route("api/ai/training")]
[ApiController]
[Authorize(Roles = "Admin")]
public class TrainingController : LocalizedControllerBase
{
    private readonly IModelTrainingService _trainingService;

    public TrainingController(IModelTrainingService trainingService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_trainingService = trainingService;
    }

    [HttpGet("status")]
    public async Task<ActionResult<ModelTrainingStatusDTO>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _trainingService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("retrain")]
    public async Task<IActionResult> Retrain(CancellationToken cancellationToken)
    {
        try
        {
            var bundle = await _trainingService.RetrainAsync(cancellationToken);
            return Ok(new
            {
                message = M("MLNETModelsRetrainedSuccessfully"),
                bundle.IsReady,
                bundle.TrainedAt,
                bundle.IntentAccuracy,
                bundle.TourCount,
                bundle.TourismCount
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
