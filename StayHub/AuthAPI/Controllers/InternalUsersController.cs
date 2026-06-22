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

    private bool IsValidServiceKey()
    {
        var expectedKey = _configuration["InternalApi:SecretKey"];
        if (string.IsNullOrWhiteSpace(expectedKey)) return false;
        
        if (!Request.Headers.TryGetValue("X-Service-Key", out var providedKey)) return false;
        
        return string.Equals(expectedKey, providedKey.ToString(), StringComparison.Ordinal);
    }
}
