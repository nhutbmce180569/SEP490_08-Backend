using AuthAPI.DTOs;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/users?page=1&pageSize=10
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var paginationResult = await _userService.GetAllUsers(page, pageSize);
            return Ok(new { message = "Users retrieved successfully.", data = paginationResult });
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userService.GetUserById(id);

            if (user == null)
            {
                // Trả về 404 nhất quán với hệ thống
                return NotFound(new { message = "User not found." });
            }

            return Ok(new { message = "User retrieved successfully.", data = user });
        }

        // GET: api/users/email/{email}
        [HttpGet("email/{email}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var user = await _userService.GetUserByEmail(email);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(new { message = "User retrieved successfully.", data = user });
        }

        // POST: api/users
        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUserByAdmin([FromForm] CreateUserDTO createUserDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid input data." });
            }

            try
            {
                var newUser = await _userService.CreateUserByAdmin(createUserDTO);
                return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, new { message = "User created successfully by Admin.", data = newUser });
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
                return BadRequest(new { message = "Invalid input data." });
            }

            var isSuccess = await _userService.UpdateUserProfile(id, updateUserDTO);

            if (!isSuccess)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(new { message = "User updated successfully. Existing sessions for this user have been revoked." });
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var isSuccess = await _userService.DeleteUser(id);

            if (!isSuccess)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(new { message = "User deleted successfully." });
        }

        // POST: api/users/batch
        [HttpPost("batch")]
        public async Task<IActionResult> GetUsersBatch([FromBody] List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                return BadRequest(new { message = "User IDs list cannot be empty." });
            }

            try
            {
                var users = await _userService.GetUsersBatchAsync(userIds);
                return Ok(new { message = "Users retrieved successfully.", data = users });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching users batch.", details = ex.Message });
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
                        message = "Search query cannot be empty.",
                        data = new { Data = new List<object>(), Total = 0, TotalPages = 0, CurrentPage = page, PageSize = pageSize }
                    });
                }

                var paginationResult = await _userService.SearchUsersAsync(query, page, pageSize, role);

                if (paginationResult.Total == 0)
                {
                    return Ok(new
                    {
                        message = $"No users found matching '{query}'.",
                        data = paginationResult
                    });
                }

                return Ok(new
                {
                    message = $"Found {paginationResult.Total} user(s) matching '{query}'.",
                    data = paginationResult
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching for users.", details = ex.Message });
            }
        }

        // PUT: api/users/{id}/status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeUserStatus(int id, [FromBody] ChangeUserStatusDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid input data." });
            }

            try
            {
                var isSuccess = await _userService.ChangeUserStatusAsync(id, dto.Status);

                if (!isSuccess)
                {
                    return NotFound(new { message = "User not found." });
                }

                string action = dto.Status == "Blocked"
                    ? "blocked. All active sessions have been instantly revoked."
                    : "unblocked and activated.";

                return Ok(new { message = $"User account has been successfully {action}" });
            }
            catch (InvalidOperationException ex)
            {
                // Bắt lỗi nếu cố tình block Admin
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while changing user status.", details = ex.Message });
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
                    return BadRequest(new { message = "Invalid filter parameters.", errors = ModelState });
                }

                var paginationResult = await _userService.FilterUsersAsync(filter);

                if (paginationResult.Total == 0)
                {
                    return Ok(new
                    {
                        message = "No users found matching the filter criteria.",
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
                    message = "An error occurred while filtering users.",
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
                return NotFound(new { message = "User profile not found." });
            }

            return Ok(new { message = "User profile retrieved successfully.", data = profile });
        }
    }
}