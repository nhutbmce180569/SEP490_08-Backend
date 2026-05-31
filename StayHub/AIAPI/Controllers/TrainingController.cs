using AIAPI.DTOs;
using AIAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIAPI.Controllers;

[Route("api/ai/training")]
[ApiController]
[Authorize(Roles = "Admin")]
public class TrainingController : ControllerBase
{
    private readonly IModelTrainingService _trainingService;

    public TrainingController(IModelTrainingService trainingService)
    {
        _trainingService = trainingService;
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
                message = "ML.NET models retrained successfully.",
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
