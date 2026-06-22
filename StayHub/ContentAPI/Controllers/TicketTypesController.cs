using ContentAPI.DTOs;
using ContentAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using StayHub.Common.Controllers;
using StayHub.Common.Resources;

namespace ContentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketTypesController : LocalizedControllerBase
    {
        private readonly ITicketTypeService _ticketTypeService;

        public TicketTypesController(ITicketTypeService ticketTypeService, IStringLocalizer<Messages> localizer)
            : base(localizer)
        {
            _ticketTypeService = ticketTypeService;
        }

        // GET: api/TicketTypes?page=1&pageSize=10&searchTerm=vip
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PaginationDTO<ReadTicketTypeDTO>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            var ticketTypes = await _ticketTypeService.GetAllTicketTypes(page, pageSize, searchTerm);
            return Ok(ticketTypes);
        }

        // GET: api/TicketTypes/active
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ReadTicketTypeDTO>>> GetActiveTicket()
        {
            var ticketTypes = await _ticketTypeService.GetActiveTicketTypes();
            return Ok(ticketTypes);
        }

        // GET: api/TicketTypes/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ReadTicketTypeDTO>> GetById(int id)
        {
            var ticketType = await _ticketTypeService.GetTicketTypeById(id);
            if (ticketType == null)
            {
                return NotFound(new { message = M("TicketTypeNotFound") });
            }

            return Ok(ticketType);
        }

        // POST: api/TicketTypes
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ReadTicketTypeDTO>> Create([FromBody] CreateTicketTypeDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var createdTicketType = await _ticketTypeService.CreateTicketType(dto);
                return CreatedAtAction(nameof(GetById), new { id = createdTicketType.Id }, createdTicketType);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/TicketTypes/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTicketTypeDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _ticketTypeService.UpdateTicketType(id, dto);
                if (!result)
                {
                    return NotFound(new { message = M("TicketTypeNotFound") });
                }

                return Ok(new { message = M("TicketTypeUpdatedSuccessfully") });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        // PATCH: api/TicketTypes/5/activate
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Activate(int id)
        {
            var result = await _ticketTypeService.ChangeTicketTypeStatus(id, true);
            if (!result)
            {
                return NotFound(new { message = M("TicketTypeNotFound") });
            }

            return Ok(new { message = M("TicketTypeActivatedSuccessfully") });
        }

        // PATCH: api/TicketTypes/5/deactivate
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var result = await _ticketTypeService.ChangeTicketTypeStatus(id, false);
            if (!result)
            {
                return NotFound(new { message = M("TicketTypeNotFound") });
            }

            return Ok(new { message = M("TicketTypeDeactivatedSuccessfully") });
        }
    }
}
