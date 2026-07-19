using System.ComponentModel.DataAnnotations;

namespace ContentAPI.DTOs
{
    public class ReadCategoryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? IconUrl { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CreateCategoryDTO
    {
        [Required(ErrorMessage = "Category name is required.")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Slug is required.")]
        public string Slug { get; set; } = null!;

        public IFormFile? IconFile { get; set; }

        public string? Description { get; set; }
        public bool? IsActive { get; set; } = false;
    }

    public class UpdateCategoryDTO
    {
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public IFormFile? IconFile { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }
}
