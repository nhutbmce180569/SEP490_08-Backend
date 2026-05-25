using AuthAPI.DTOs;
using AuthAPI.Services;
using AuthAPI.Services.Implements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid input data." });
            }

            var response = await _authService.Login(loginDTO);

            if (response == null)
            {
                return Unauthorized(new { message = "Invalid email or password, or account is not active." });
            }

            return Ok(new { message = "Login successful.", data = response });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDTO googleLoginDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid input data." });
            }

            var response = await _authService.GoogleLogin(googleLoginDTO);

            if (response == null)
            {
                return Unauthorized(new { message = "Invalid Google token or account is locked." });
            }

            return Ok(new { message = "Google login successful.", data = response });
        }

        [HttpPost("facebook-login")]
        public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginDTO request)
        {
            if (string.IsNullOrEmpty(request.AccessToken))
            {
                return BadRequest(new { message = "Access token is required." });
            }

            var response = await _authService.FacebookLogin(request.AccessToken);

            if (response == null)
            {
                return Unauthorized(new { message = "Invalid Facebook token or account is locked." });
            }

            return Ok(new { message = "Facebook login successful.", data = response });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid registration data." });
            }

            var response = await _authService.Register(registerDTO);

            if (response == null)
            {
                return Conflict(new { message = "Email is already in use." });
            }

            return Ok(new { message = "Registration successful.", data = response });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required." });
            }

            var response = await _authService.RefreshToken(request.RefreshToken);

            if (response == null)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token. Please login again." });
            }

            return Ok(new { message = "Token refreshed successfully.", data = response });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required." });
            }

            await _authService.Logout(request.RefreshToken);

            return Ok(new { message = "Logged out successfully." });
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid token claims." });
            }

            var profile = await _authService.GetProfileAsync(userId);
            if (profile == null)
            {
                return NotFound(new { message = "User not found or account is inactive." });
            }

            return Ok(new { message = "Profile retrieved successfully.", data = profile });
        }

        [HttpPut("profile")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDTO dto) // ĐÃ SỬA: Dùng FromForm thay vì FromBody
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid input data." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid token claims." });
            }

            var response = await _authService.UpdateProfileAsync(userId, dto);
            if (response == null)
            {
                return BadRequest(new { message = "Failed to update profile. User not found or inactive." });
            }

            return Ok(new
            {
                message = "Profile updated successfully. Fresh tokens issued to update client claims.",
                data = response
            });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid token claims." });
            }

            var response = await _authService.ChangePassword(userId, dto);

            if (response == null)
            {
                return BadRequest(new { message = "Password change failed. Incorrect old password or invalid account." });
            }

            return Ok(new
            {
                message = "Password changed successfully. All other devices have been logged out.",
                data = response
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid input data." });
            }

            await _authService.ForgotPassword(dto);

            return Ok(new { message = "If the email is registered, a password reset code has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid input data." });
            }

            var isSuccess = await _authService.ResetPassword(dto);

            if (!isSuccess)
            {
                return BadRequest(new { message = "Invalid or expired verification code, or account is not eligible." });
            }

            return Ok(new { message = "Password has been reset successfully. All other devices have been logged out. You can now login." });
        }
    }
}