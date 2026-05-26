using System.ComponentModel.DataAnnotations;
using TourAPI.Models;

namespace TourAPI.DTOs
{
    public class ReadTourDTO
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public string? Status { get; set; }

        public string? ImageUrl { get; set; }

        public string? Country { get; set; }

        public string? City { get; set; }

        public string? Address { get; set; }
        public double? AverageStar { get; set; }

        public ICollection<ReadTourItineraryDTO>? TourItineraries { get; set; }

        public ICollection<ReadTourScheduleDTO>? TourSchedules { get; set; }

        public ICollection<ReadReviewDTO>? Reviews { get; set; }
    }
    public class ReadTourBasicDTO
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? ImageUrl { get; set; }

        public string? Country { get; set; }

        public string? City { get; set; }
    }
    public abstract class BaseTourDTO
    {
        [Required(ErrorMessage = "OperatorId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "OperatorId must be greater than 0")]
        public int OperatorId { get; set; }

        [Required(ErrorMessage = "CategoryId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be greater than 0")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Country is required")]
        public string? Country { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string? City { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public string? Address { get; set; }

        public string? Status { get; set; } = "Inactive";
        public IFormFile? Image { get; set; }
    }

    public class CreateTourDTO : BaseTourDTO
    {
    }

    public class UpdateTourDTO : BaseTourDTO
    {
        public bool RemoveImage { get; set; }
    }

    public class ReadUserDTO
    {
        public string? FullName { get; set; }
        public string? Avatar { get; set; }
    }

    public class UserApiResponse
    {
        public string? Message { get; set; }
        public ReadUserDTO? Data { get; set; }
    }

    // Thêm class này vào cuối file, trước dấu ngoặc nhọn đóng của namespace
    public class UpdateTourStatusRequest
    {
        public string Status { get; set; } = null!;
    }
}
