using AuthAPI.DTOs;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers
{
    /// <summary>Internal customer analytics endpoints for cross-service aggregation (Manager/Admin only).</summary>
    [Route("api/internal/analytics/customers")]
    [ApiController]
    [Authorize(Roles = "Manager,Admin")]
    public class InternalCustomerAnalyticsController : ControllerBase
    {
        private readonly IUserService _userService;

        public InternalCustomerAnalyticsController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("demographics")]
        public async Task<IActionResult> GetDemographics(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string granularity = "day")
        {
            try
            {
                var result = await _userService.GetCustomerDemographicsAsync(from, to, granularity);
                return Ok(new { message = "Customer demographics retrieved successfully.", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve customer demographics.", details = ex.Message });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetCustomerList(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _userService.GetCustomersForAnalyticsAsync(search, page, pageSize);
                return Ok(new { message = "Customer list retrieved successfully.", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve customer list.", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomerSummary(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null || !user.Roles.Contains("Customer"))
            {
                return NotFound(new { message = "Customer not found." });
            }

            return Ok(new
            {
                message = "Customer summary retrieved successfully.",
                data = new CustomerSummaryDTO
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    AvatarUrl = user.AvatarUrl,
                    Provider = user.Provider,
                    Gender = user.Gender,
                    DateOfBirth = user.DateOfBirth,
                    Status = user.Status,
                    LastOnline = user.LastOnline,
                    CreatedAt = user.CreatedAt
                }
            });
        }
    }
}
