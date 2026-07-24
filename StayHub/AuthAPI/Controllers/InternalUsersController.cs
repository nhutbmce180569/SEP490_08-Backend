using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers;

[AllowAnonymous]
[Route("api/internal/users")]
[ApiController]
public class InternalUsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;

    public InternalUsersController(IUserService userService, IConfiguration configuration)
    {
        _userService = userService;
        _configuration = configuration;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        if (!IsValidServiceKey())
        {
            return Unauthorized(new { message = "Invalid service key" });
        }

        var user = await _userService.GetUserById(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(new { message = "User retrieved successfully", data = user });
    }

    [HttpGet("birthdays")]
    public async Task<IActionResult> GetUsersByBirthdayMonth([FromQuery] int month)
    {
        if (!IsValidServiceKey())
        {
            return Unauthorized(new { message = "Invalid service key" });
        }

        if (month < 1 || month > 12)
        {
            return BadRequest(new { message = "Invalid month" });
        }

        var users = await _userService.GetCustomersByBirthdayMonthAsync(month);
        return Ok(users);
    }

    [HttpGet("active-customers/ids")]
    public async Task<IActionResult> GetAllActiveCustomerIds()
    {
        if (!IsValidServiceKey())
        {
            return Unauthorized(new { message = "Invalid service key" });
        }

        var ids = await _userService.GetAllActiveCustomerIdsAsync();
        return Ok(ids);
    }

    [HttpPost("batch")]
    public async Task<IActionResult> GetUsersBatch([FromBody] List<int> ids)
    {
        if (!IsValidServiceKey())
        {
            return Unauthorized(new { message = "Invalid service key" });
        }

        if (ids == null || !ids.Any())
        {
            return Ok(new List<AuthAPI.DTOs.UserSearchResultDto>());
        }

        var users = await _userService.GetUsersBatchAsync(ids);
        return Ok(users);
    }

    [HttpGet("by-role")]
    public async Task<IActionResult> GetUsersByRole([FromQuery] string role)
    {
        if (!IsValidServiceKey())
        {
            return Unauthorized(new { message = "Invalid service key" });
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            return BadRequest(new { message = "Role is required" });
        }

        var users = await _userService.GetUsersByRoleAsync(role);
        return Ok(users);
    }

    private bool IsValidServiceKey()
    {
        if (!Request.Headers.TryGetValue("X-Service-Key", out var providedKey)) return false;

        var key1 = _configuration["InternalApi:SecretKey"];
        var key2 = _configuration["InternalService:Key"];

        if (!string.IsNullOrWhiteSpace(key1) && string.Equals(key1, providedKey.ToString(), StringComparison.Ordinal))
            return true;

        if (!string.IsNullOrWhiteSpace(key2) && string.Equals(key2, providedKey.ToString(), StringComparison.Ordinal))
            return true;

        return false;
    }
}
