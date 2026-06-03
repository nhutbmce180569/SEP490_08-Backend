using AuthAPI.DTOs;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;
using System.Security.Claims;

namespace AuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : LocalizedControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = M("InvalidInputData") });

            var response = await _authService.Login(loginDTO);
            if (response == null)
                return Unauthorized(new { message = M("InvalidEmailOrPassword") });

            return Ok(new { message = M("LoginSuccessful"), data = response });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDTO googleLoginDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = M("InvalidInputData") });

            var response = await _authService.GoogleLogin(googleLoginDTO);
            if (response == null)
                return Unauthorized(new { message = M("InvalidGoogleToken") });

            return Ok(new { message = M("GoogleLoginSuccessful"), data = response });
        }

        [HttpPost("facebook-login")]
        public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginDTO request)
        {
            if (string.IsNullOrEmpty(request.AccessToken))
                return BadRequest(new { message = M("AccessTokenRequired") });

            var response = await _authService.FacebookLogin(request.AccessToken);
            if (response == null)
                return Unauthorized(new { message = M("InvalidFacebookToken") });

            return Ok(new { message = M("FacebookLoginSuccessful"), data = response });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = M("InvalidRegistrationData") });

            var response = await _authService.Register(registerDTO);
            if (response == null)
                return Conflict(new { message = M("EmailAlreadyInUse") });

            return Ok(new { message = M("RegistrationSuccessful"), data = response });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
                return BadRequest(new { message = M("RefreshTokenRequired") });

            var response = await _authService.RefreshToken(request.RefreshToken);
            if (response == null)
                return Unauthorized(new { message = M("InvalidRefreshToken") });

            return Ok(new { message = M("TokenRefreshedSuccessfully"), data = response });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
                return BadRequest(new { message = M("RefreshTokenRequired") });

            await _authService.Logout(request.RefreshToken);
            return Ok(new { message = M("LoggedOutSuccessfully") });
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { message = M("InvalidTokenClaims") });

            var profile = await _authService.GetProfileAsync(userId);
            if (profile == null)
                return NotFound(new { message = M("UserNotFoundOrInactive") });

            return Ok(new { message = M("ProfileRetrievedSuccessfully"), data = profile });
        }

        [HttpPut("profile")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = M("InvalidInputData") });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { message = M("InvalidTokenClaims") });

            var response = await _authService.UpdateProfileAsync(userId, dto);
            if (response == null)
                return BadRequest(new { message = M("FailedToUpdateProfile") });

            return Ok(new { message = M("ProfileUpdatedSuccessfully"), data = response });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { message = M("InvalidTokenClaims") });

            var response = await _authService.ChangePassword(userId, dto);
            if (response == null)
                return BadRequest(new { message = M("PasswordChangeFailed") });

            return Ok(new { message = M("PasswordChangedSuccessfully"), data = response });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = M("InvalidInputData") });

            await _authService.ForgotPassword(dto);
            return Ok(new { message = M("ForgotPasswordSuccess") });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = M("InvalidInputData") });

            var isSuccess = await _authService.ResetPassword(dto);
            if (!isSuccess)
                return BadRequest(new { message = M("ResetPasswordFailed") });

            return Ok(new { message = M("PasswordResetSuccessfully") });
        }
    }
}
