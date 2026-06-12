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
    public class UsersController : LocalizedControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        public UsersController(IUserService userService, IStringLocalizer<Messages> localizer, IConfiguration configuration)
            : base(localizer)
        {
            _userService = userService;
            _configuration = configuration;
        }

        // GET: api/users?page=1&pageSize=10
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var paginationResult = await _userService.GetAllUsers(page, pageSize);
            return Ok(new { message = M("UsersRetrievedSuccessfully"), data = paginationResult });
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, Manager, Staff")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userService.GetUserById(id);

            if (user == null)
            {
                // Trả về 404 nhất quán với hệ thống
                return NotFound(new { message = M("UserNotFound") });
            }

            return Ok(new { message = M("UserRetrievedSuccessfully"), data = user });
        }

        // GET: api/users/email/{email}
        [HttpGet("email/{email}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var user = await _userService.GetUserByEmail(email);

            if (user == null)
            {
                return NotFound(new { message = M("UserNotFound") });
            }

            return Ok(new { message = M("UserRetrievedSuccessfully"), data = user });
        }

        // POST: api/users
        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUserByAdmin([FromForm] CreateUserDTO createUserDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = M("InvalidInputData") });
            }

            try
            {
                var result = await _userService.CreateUserByAdmin(createUserDTO);
                return CreatedAtAction(
                    nameof(GetUserById),
                    new { id = result.User.Id },
                    new { message = M("UserCreatedSuccessfullyByAdmin"), data = result });
            }
            catch (Exception ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(int id, [FromForm] UpdateUserDTO updateUserDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = M("InvalidInputData") });
            }

            var isSuccess = await _userService.UpdateUserProfile(id, updateUserDTO);

            if (!isSuccess)
            {
                return NotFound(new { message = M("UserNotFound") });
            }

            return Ok(new { message = M("UserUpdatedSuccessfullyExistingSessionsForThisUserHaveBeen") });
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var isSuccess = await _userService.DeleteUser(id);

            if (!isSuccess)
            {
                return NotFound(new { message = M("UserNotFound") });
            }

            return Ok(new { message = M("UserDeletedSuccessfully") });
        }

        // POST: api/users/batch
        [HttpPost("batch")]
        public async Task<IActionResult> GetUsersBatch([FromBody] List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                return BadRequest(new { message = M("UserIDsListCannotBeEmpty") });
            }

            try
            {
                var users = await _userService.GetUsersBatchAsync(userIds);
                return Ok(new { message = M("UsersRetrievedSuccessfully"), data = users });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("AnErrorOccurredWhileFetchingUsersBatch"), details = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("batch/public")]
        public async Task<IActionResult> GetUsersBatchPublic([FromBody] List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
                return BadRequest(new { message = M("UserIDsListCannotBeEmpty") });

            try
            {
                var users = await _userService.GetUsersBatchAsync(userIds);

                var safeData = users.Select(u => new {
                    Id = u.Id,
                    FullName = u.FullName,
                    AvatarUrl = u.AvatarUrl
                });

                return Ok(new { message = M("UsersRetrievedSuccessfully"), data = safeData });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("AnErrorOccurredWhileFetchingUsersBatch"), details = ex.Message });
            }
        }

        // GET: api/users/search
        [HttpGet("search")]
        [Authorize]
        public async Task<IActionResult> SearchUsers(
            [FromQuery(Name = "q")] string query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? role = null)
        {
            try
            {
                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 10;

                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new
                    {
                        message = M("SearchQueryCannotBeEmpty"),
                        data = new { Data = new List<object>(), Total = 0, TotalPages = 0, CurrentPage = page, PageSize = pageSize }
                    });
                }

                var paginationResult = await _userService.SearchUsersAsync(query, page, pageSize, role);

                if (paginationResult.Total == 0)
                {
                    return Ok(new
                    {
                        message = string.Format(M("NoUsersFoundMatchingQuery"), query),
                        data = paginationResult
                    });
                }

                return Ok(new
                {
                    message = string.Format(M("FoundUsersMatchingQuery"), paginationResult.Total, query),
                    data = paginationResult
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("AnErrorOccurredWhileSearchingForUsers"), details = ex.Message });
            }
        }

        // PUT: api/users/{id}/status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeUserStatus(int id, [FromBody] ChangeUserStatusDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = M("InvalidInputData") });
            }

            try
            {
                var isSuccess = await _userService.ChangeUserStatusAsync(id, dto.Status);

                if (!isSuccess)
                {
                    return NotFound(new { message = M("UserNotFound") });
                }

                var statusMessage = dto.Status == "Blocked"
                    ? M("UserAccountBlockedSuccess")
                    : M("UserAccountUnblockedSuccess");

                return Ok(new { message = statusMessage });
            }
            catch (InvalidOperationException ex)
            {
                // Bắt lỗi nếu cố tình block Admin
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = M("AnErrorOccurredWhileChangingUserStatus"), details = ex.Message });
            }
        }

        // GET: api/users/filter?FullName=John&Roles=Admin&Roles=Customer&Page=1&PageSize=10
        [HttpGet("filter")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FilterUsers([FromQuery] UserFilterDTO filter)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = M("InvalidFilterParameters"), errors = ModelState });
                }

                var paginationResult = await _userService.FilterUsersAsync(filter);

                if (paginationResult.Total == 0)
                {
                    return Ok(new
                    {
                        message = M("NoUsersFoundMatchingTheFilterCriteria"),
                        data = paginationResult
                    });
                }

                return Ok(new
                {
                    message = $"Found {paginationResult.Total} user(s) matching the criteria.",
                    data = paginationResult
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = M("AnErrorOccurredWhileFilteringUsers"),
                    details = ex.Message
                });
            }
        }

        // GET: api/users/{id}/profile
        [HttpGet("{id}/profile")]
        public async Task<IActionResult> GetUserProfile(int id)
        {
            var profile = await _userService.GetUserProfileAsync(id);

            if (profile == null)
            {
                return NotFound(new { message = M("UserProfileNotFound") });
            }

            return Ok(new { message = M("UserProfileRetrievedSuccessfully"), data = profile });
        }

        // GET: api/users/internal/{id}/fcm-token
        [HttpGet("internal/{id}/fcm-token")]
        public async Task<IActionResult> GetFcmTokenInternal(int id, [FromHeader(Name = "X-Internal-Key")] string? internalKey)
        {
            if (internalKey != _configuration["InternalApi:SecretKey"])
                return Unauthorized();

            var token = await _userService.GetFcmTokenAsync(id);
            return Ok(new { token = token });
        }

        // PUT: api/users/fcm-token
        [HttpPut("fcm-token")]
        [Authorize] 
        public async Task<IActionResult> UpdateFcmToken([FromBody] UpdateFcmTokenDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = M("InvalidInputData") });
            }

            // Lấy UserId từ JWT Token của người đang đăng nhập
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("id")?.Value;

            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized(new { message = M("InvalidUserToken") });
            }

            try
            {
                var isSuccess = await _userService.UpdateFcmTokenAsync(userId, dto.FcmToken);

                if (!isSuccess)
                {
                    return NotFound(new { message = M("UserNotFound") });
                }

                // Tái sử dụng Localization hoặc trả về text cứng
                return Ok(new { message = "FCM Token updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating FCM Token", details = ex.Message });
            }
        }

        [HttpDelete("internal/{id}/fcm-token")]
        public async Task<IActionResult> ClearFcmTokenInternal(int id, [FromHeader(Name = "X-Internal-Key")] string? internalKey)
        {
            if (internalKey != _configuration["InternalApi:SecretKey"])
                return Unauthorized();

            await _userService.UpdateFcmTokenAsync(id, null); // set null = xóa token
            return Ok();
        }
    }
}
