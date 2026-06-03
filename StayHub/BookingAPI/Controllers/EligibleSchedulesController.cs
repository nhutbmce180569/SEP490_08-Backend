using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookingAPI.Services; 

namespace BookingAPI.Controllers
{
    // ĐIỂM QUAN TRỌNG 1: Đổi Route thành api/orders để Gateway tự động cho qua
    [Route("api/orders")]
    [ApiController]
    // ĐIỂM QUAN TRỌNG 2: Đổi tên Controller để không đụng hàng với AuthAPI
    public class EligibleSchedulesController : LocalizedControllerBase 
    {
        private readonly IEligibleScheduleService _eligibleScheduleService;

        public EligibleSchedulesController(IEligibleScheduleService eligibleScheduleService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_eligibleScheduleService = eligibleScheduleService;
        }

        [Authorize]
        [HttpGet("me/eligible-schedules")] 
        public async Task<IActionResult> GetMyEligibleSchedules()
        {
            try
            {
                // Bóc tách userId từ Token
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? User.FindFirst("id")?.Value
                                   ?? User.FindFirst("sub")?.Value;

                if (!int.TryParse(userIdString, out int userId))
                {
                    return Unauthorized(new { message = M("InvalidTokenMissingOrInvalidUserID") });
                }

                var schedules = await _eligibleScheduleService.GetEligibleSchedulesAsync(userId);

                return Ok(new
                {
                    message = M("EligibleSchedulesRetrievedSuccessfully"),
                    data = schedules
                });
            }
            catch (Exception ex)
            {
                // Bắt Exception và ném lỗi chuẩn 500
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}