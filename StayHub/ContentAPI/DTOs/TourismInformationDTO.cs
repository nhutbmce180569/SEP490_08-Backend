using System.ComponentModel.DataAnnotations;

namespace ContentAPI.DTOs
{
    public class ReadTourismInformationDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? ImageUrl { get; set; }
        public string? SourceName { get; set; }
        public string? SourceUrl { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateTourismInformationDTO
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(255, ErrorMessage = "Name cannot exceed 255 characters.")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Type is required.")]
        [MaxLength(50, ErrorMessage = "Type cannot exceed 50 characters.")]
        public string Type { get; set; } = null!;

        public string? Description { get; set; }

        [MaxLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        public string? Address { get; set; }

        [MaxLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; }

        [MaxLength(100, ErrorMessage = "Country cannot exceed 100 characters.")]
        public string? Country { get; set; }

        public string? Latitude { get; set; }

        public string? Longitude { get; set; }

        public IFormFile? ImageFile { get; set; }

        [MaxLength(255, ErrorMessage = "Source name cannot exceed 255 characters.")]
        public string? SourceName { get; set; }

        public string? SourceUrl { get; set; }
    }

    public class UpdateTourismInformationDTO
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(255, ErrorMessage = "Name cannot exceed 255 characters.")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Type is required.")]
        [MaxLength(50, ErrorMessage = "Type cannot exceed 50 characters.")]
        public string Type { get; set; } = null!;

        public string? Description { get; set; }

        [MaxLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        public string? Address { get; set; }

        [MaxLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; }

        [MaxLength(100, ErrorMessage = "Country cannot exceed 100 characters.")]
        public string? Country { get; set; }

        public string? Latitude { get; set; }

        public string? Longitude { get; set; }

        public IFormFile? ImageFile { get; set; }
        
        public bool RemoveImage { get; set; }

        [MaxLength(255, ErrorMessage = "Source name cannot exceed 255 characters.")]
        public string? SourceName { get; set; }

        public string? SourceUrl { get; set; }
    }
}
