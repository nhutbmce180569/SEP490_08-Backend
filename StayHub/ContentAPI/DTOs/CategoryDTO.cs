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
        [MaxLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Slug is required.")]
        [MaxLength(100, ErrorMessage = "Slug cannot exceed 100 characters.")]
        public string Slug { get; set; } = null!;

        public IFormFile? IconFile { get; set; }

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }
    }

    public class UpdateCategoryDTO
    {
        [MaxLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        public string Name { get; set; } = null!;
        [MaxLength(100, ErrorMessage = "Slug cannot exceed 100 characters.")]
        public string Slug { get; set; } = null!;
        public IFormFile? IconFile { get; set; }
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }
}
