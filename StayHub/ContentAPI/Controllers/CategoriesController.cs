using ContentAPI.DTOs;
using ContentAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: api/Categories?page=1&pageSize=10
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PaginationDTO<ReadCategoryDTO>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var categories = await _categoryService.GetAllCategories(page, pageSize);
            return Ok(categories);
        }

        // GET: api/Categories/active?page=1&pageSize=10
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<ActionResult<PaginationDTO<ReadCategoryDTO>>> GetActive([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var categories = await _categoryService.GetActiveCategories(page, pageSize);
            return Ok(categories);
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ReadCategoryDTO>> GetById(int id)
        {
            var category = await _categoryService.GetCategoryById(id);
            if (category == null)
            {
                // FE hiển thị: "Category not found."
                return NotFound(new { message = "Category not found." });
            }
            return Ok(category);
        }

        // POST: api/Categories
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ReadCategoryDTO>> Create([FromForm] CreateCategoryDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdCategory = await _categoryService.CreateCategory(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdCategory.Id }, createdCategory);
        }

        // PUT: api/Categories/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateCategoryDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _categoryService.UpdateCategory(id, dto);
            if (!result)
            {
                return NotFound(new { message = "Category not found or has been deleted." });
            }

            return Ok(new { message = "Category updated successfully." });
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _categoryService.DeleteCategory(id);

                if (!result)
                {
                    return NotFound(new { message = "Category not found." });
                }

                // Delete thành công thường trả về 204 NoContent, không cần body
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                // FE sẽ nhận được lỗi 400 kèm message từ Service
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                // Lỗi 500 nên ẩn chi tiết kỹ thuật (ex.Message) với người dùng cuối, trả về câu báo lỗi chung chung
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while processing your request. Please try again later." });
            }
        }

        // PATCH: api/Categories/5/activate
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Activate(int id)
        {
            var result = await _categoryService.ChangeCategoryStatus(id, true);
            if (!result)
            {
                return NotFound(new { message = "Category not found." });
            }

            return Ok(new { message = "Category activated successfully." });
        }

        // PATCH: api/Categories/5/deactivate
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var result = await _categoryService.ChangeCategoryStatus(id, false);
            if (!result)
            {
                return NotFound(new { message = "Category not found." });
            }

            return Ok(new { message = "Category deactivated successfully." });
        }

        // GET: api/Categories/search?q=hotel&page=1&pageSize=10
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(
            [FromQuery(Name = "q")] string keyword,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return BadRequest(new
                    {
                        message = "Search keyword cannot be empty.",
                        data = new PaginationDTO<ReadCategoryDTO>
                        {
                            Data = new List<ReadCategoryDTO>(),
                            Total = 0,
                            TotalPages = 0,
                            CurrentPage = page,
                            PageSize = pageSize
                        }
                    });
                }

                var result = await _categoryService.SearchCategoriesAsync(keyword, page, pageSize);

                if (result.Total == 0)
                {
                    return Ok(new
                    {
                        message = $"No categories found matching '{keyword}'.",
                        data = result
                    });
                }

                return Ok(new
                {
                    message = $"Found {result.Total} category(ies) matching '{keyword}'.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while searching for categories.",
                    details = ex.Message
                });
            }
        }
    }
}