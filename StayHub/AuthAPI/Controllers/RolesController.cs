using AuthAPI.DTOs;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        // GET: api/roles
        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleService.GetAllRoles();
            return Ok(new { message = "Roles retrieved successfully.", data = roles });
        }

        // GET: api/roles/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetRoleById(int id)
        {
            var role = await _roleService.GetRoleById(id);

            if (role == null)
            {
                return NotFound(new { message = $"Role with ID {id} not found." });
            }

            return Ok(new { message = "Role retrieved successfully.", data = role });
        }

        // GET: api/roles/name/{name}
        [HttpGet("name/{name}")]
        public async Task<IActionResult> GetRoleByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = "Role name cannot be empty." });
            }

            var role = await _roleService.GetRoleByName(name);

            if (role == null)
            {
                return NotFound(new { message = $"Role with name '{name}' not found." });
            }

            return Ok(new { message = "Role retrieved successfully.", data = role });
        }
    }
}