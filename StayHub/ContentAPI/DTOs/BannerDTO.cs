using System.ComponentModel.DataAnnotations;

namespace ContentAPI.DTOs
{
    public class ReadBannerDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public string? TargetUrl { get; set; }
        public int? Priority { get; set; }
        public bool? IsActive { get; set; }
    }
    public class CreateBannerDTO
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Banner image is required.")]
        public IFormFile ImageFile { get; set; } = null!;

        [MaxLength(500, ErrorMessage = "Target URL cannot exceed 500 characters.")]
        public string? TargetUrl { get; set; }

        public int? Priority { get; set; } = 0;
    }
    public class UpdateBannerDTO
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
        public string Title { get; set; } = null!;

        public IFormFile? ImageFile { get; set; }

        [MaxLength(500, ErrorMessage = "Target URL cannot exceed 500 characters.")]
        public string? TargetUrl { get; set; }

        public int? Priority { get; set; }

        public bool? IsActive { get; set; }
    }
}
