using ContentAPI.DTOs;
using ContentAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketTypesController : ControllerBase
    {
        private readonly ITicketTypeService _ticketTypeService;

        public TicketTypesController(ITicketTypeService ticketTypeService)
        {
            _ticketTypeService = ticketTypeService;
        }

        // GET: api/TicketTypes?page=1&pageSize=10
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PaginationDTO<ReadTicketTypeDTO>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var ticketTypes = await _ticketTypeService.GetAllTicketTypes(page, pageSize);
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
                return NotFound(new { message = "Ticket type not found." });
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

            var createdTicketType = await _ticketTypeService.CreateTicketType(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdTicketType.Id }, createdTicketType);
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

            var result = await _ticketTypeService.UpdateTicketType(id, dto);
            if (!result)
            {
                return NotFound(new { message = "Ticket type not found." });
            }

            return Ok(new { message = "Ticket type updated successfully." });
        }

        // PATCH: api/TicketTypes/5/activate
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Activate(int id)
        {
            var result = await _ticketTypeService.ChangeTicketTypeStatus(id, true);
            if (!result)
            {
                return NotFound(new { message = "Ticket type not found." });
            }

            return Ok(new { message = "Ticket type activated successfully." });
        }

        // PATCH: api/TicketTypes/5/deactivate
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var result = await _ticketTypeService.ChangeTicketTypeStatus(id, false);
            if (!result)
            {
                return NotFound(new { message = "Ticket type not found." });
            }

            return Ok(new { message = "Ticket type deactivated successfully." });
        }
    }
}
