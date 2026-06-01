using ContentAPI.DTOs;
using ContentAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TourismInformationController : ControllerBase
    {
        private readonly ITourismInformationService _tourismInformationService;

        public TourismInformationController(ITourismInformationService tourismInformationService)
        {
            _tourismInformationService = tourismInformationService;
        }

        // GET: api/TourismInformation?page=1&pageSize=10&searchTerm=beach&type=Destination&status=Active&city=Da Lat
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PaginationDTO<ReadTourismInformationDTO>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? type = null,
            [FromQuery] string? status = null,
            [FromQuery] string? city = null)
        {
            try
            {
                var result = await _tourismInformationService.GetAllAsync(
                    page, pageSize, searchTerm, type, status, city);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/TourismInformation/active?page=1&pageSize=10
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<ActionResult<PaginationDTO<ReadTourismInformationDTO>>> GetActive(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _tourismInformationService.GetActiveAsync(page, pageSize);
            return Ok(result);
        }

        // GET: api/TourismInformation/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ReadTourismInformationDTO>> GetById(int id)
        {
            var tourismInfo = await _tourismInformationService.GetByIdAsync(id);
            if (tourismInfo == null)
            {
                return NotFound(new { message = "Tourism information not found." });
            }

            return Ok(tourismInfo);
        }

        // POST: api/TourismInformation
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ReadTourismInformationDTO>> Create([FromForm] CreateTourismInformationDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var created = await _tourismInformationService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/TourismInformation/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateTourismInformationDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _tourismInformationService.UpdateAsync(id, dto);
                if (!result)
                {
                    return NotFound(new { message = "Tourism information not found." });
                }

                return Ok(new { message = "Tourism information updated successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PATCH: api/TourismInformation/5/activate
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var result = await _tourismInformationService.ChangeStatusAsync(id, true);
                if (!result)
                {
                    return NotFound(new { message = "Tourism information not found." });
                }

                return Ok(new { message = "Tourism information activated successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PATCH: api/TourismInformation/5/deactivate
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var result = await _tourismInformationService.ChangeStatusAsync(id, false);
                if (!result)
                {
                    return NotFound(new { message = "Tourism information not found." });
                }

                return Ok(new { message = "Tourism information deactivated successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
