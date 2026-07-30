using System.ComponentModel.DataAnnotations;

namespace ContentAPI.DTOs
{
    public class ReadTicketTypeDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateTicketTypeDTO
    {
        [Required(ErrorMessage = "Ticket type name is required.")]
        [MaxLength(50, ErrorMessage = "Ticket type name cannot exceed 50 characters.")]
        [RegularExpression(@"^[\p{L}\p{N}\s]*$", ErrorMessage = "Ticket type name cannot contain special characters.")]
        public string Name { get; set; } = null!;

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Range(0, 150, ErrorMessage = "Min age must be a positive number.")]
        public int? MinAge { get; set; }
        
        [Range(0, 150, ErrorMessage = "Max age must be a positive number.")]
        public int? MaxAge { get; set; }

        public bool? IsActive { get; set; } = true;
    }

    public class UpdateTicketTypeDTO
    {
        [Required(ErrorMessage = "Ticket type name is required.")]
        [MaxLength(50, ErrorMessage = "Ticket type name cannot exceed 50 characters.")]
        [RegularExpression(@"^[\p{L}\p{N}\s]*$", ErrorMessage = "Ticket type name cannot contain special characters.")]
        public string Name { get; set; } = null!;

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Range(0, 150, ErrorMessage = "Min age must be a positive number.")]
        public int? MinAge { get; set; }
        
        [Range(0, 150, ErrorMessage = "Max age must be a positive number.")]
        public int? MaxAge { get; set; }

        public bool? IsActive { get; set; }
    }
}
