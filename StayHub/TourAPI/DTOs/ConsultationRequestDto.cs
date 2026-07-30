using System.ComponentModel.DataAnnotations;

namespace TourAPI.DTOs
{
    public class ConsultationRequestDto
    {
        [Required(ErrorMessage = "Tour ID is required.")]
        public int TourId { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Phone is required.")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email format.")]
        public string Email { get; set; } = null!;

        public string? Note { get; set; }
    }
}
