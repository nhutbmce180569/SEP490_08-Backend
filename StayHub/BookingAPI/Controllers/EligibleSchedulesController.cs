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
    [Route("api/orders")]
    [ApiController]
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
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}