using AIAPI.DTOs;
using AIAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIAPI.Controllers;

[Route("api/ai/review-analysis")]
[ApiController]
public class ReviewAnalysisController : ControllerBase
{
    private readonly IReviewAnalysisService _reviewAnalysisService;
    private readonly ILogger<ReviewAnalysisController> _logger;

    public ReviewAnalysisController(
        IReviewAnalysisService reviewAnalysisService,
        ILogger<ReviewAnalysisController> logger)
    {
        _reviewAnalysisService = reviewAnalysisService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("analyze")]
    public async Task<ActionResult<ReviewAnalysisResponseDTO>> AnalyzeReview([FromBody] ReviewAnalysisRequestDTO request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _reviewAnalysisService.AnalyzeReviewAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ngoại lệ bắt được ở ReviewAnalysisController.");
            return StatusCode(500, new 
            { 
                Message = "Đã xảy ra sự cố khi phân tích đánh giá bằng AI. Vui lòng thử lại sau.", 
                ErrorDetail = ex.Message 
            });
        }
    }
}
