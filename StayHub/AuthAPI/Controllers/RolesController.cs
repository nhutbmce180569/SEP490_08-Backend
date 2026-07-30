using AuthAPI.DTOs;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;

namespace AuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : LocalizedControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {_roleService = roleService;
        }

        // GET: api/roles
        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleService.GetAllRoles();
            return Ok(new { message = M("RolesRetrievedSuccessfully"), data = roles });
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

            return Ok(new { message = M("RoleRetrievedSuccessfully"), data = role });
        }

        // GET: api/roles/name/{name}
        [HttpGet("name/{name}")]
        public async Task<IActionResult> GetRoleByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { message = M("RoleNameCannotBeEmpty") });
            }

            var role = await _roleService.GetRoleByName(name);

            if (role == null)
            {
                return NotFound(new { message = $"Role with name '{name}' not found." });
            }

            return Ok(new { message = M("RoleRetrievedSuccessfully"), data = role });
        }
    }
}