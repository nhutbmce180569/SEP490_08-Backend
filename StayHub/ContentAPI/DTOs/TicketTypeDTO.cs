using System.ComponentModel.DataAnnotations;

namespace ContentAPI.DTOs
{
    public class ReadTicketTypeDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateTicketTypeDTO
    {
        [Required(ErrorMessage = "Ticket type name is required.")]
        [MaxLength(100, ErrorMessage = "Ticket type name cannot exceed 100 characters.")]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public bool? IsActive { get; set; } = true;
    }

    public class UpdateTicketTypeDTO
    {
        [Required(ErrorMessage = "Ticket type name is required.")]
        [MaxLength(100, ErrorMessage = "Ticket type name cannot exceed 100 characters.")]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public bool? IsActive { get; set; }
    }
}
