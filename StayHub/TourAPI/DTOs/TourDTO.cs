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

        public string? Country { get; set; }

        public string? City { get; set; }

        public string? Address { get; set; }

        public string? ImageUrl { get; set; }

        public string? TransportationType { get; set; }

        public string? SourceName { get; set; }

        public string? SourceUrl { get; set; }

        public int CreatedBy { get; set; }
        public string? CreatedByName { get; set; }

        public int? UpdatedBy { get; set; }
        public string? UpdatedByName { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? Status { get; set; }

        public double? AverageStar { get; set; }
        
        public int TotalReviews { get; set; }
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }

        public bool CanEdit { get; set; }

        public ICollection<ReadTourItineraryDTO>? TourItineraries { get; set; }

        public ICollection<ReadTourScheduleDTO>? TourSchedules { get; set; }

        public ICollection<ReadReviewDTO>? Reviews { get; set; }

        public ICollection<ReadTourImageDTO>? TourImages { get; set; }
    }

    public class ReadTourImageDTO
    {
        public int Id { get; set; }
        public int TourId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
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
        [Required(ErrorMessage = "CategoryId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be greater than 0")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Name must be between 5 and 200 characters")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Description is required")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Country is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Country must be between 2 and 100 characters")]
        public string? Country { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "City must be between 2 and 100 characters")]
        public string? City { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(255, MinimumLength = 5, ErrorMessage = "Address must be between 5 and 255 characters")]
        public string? Address { get; set; }

        public string? Status { get; set; } = "Inactive";

        public IFormFile? Image { get; set; }

        [Required(ErrorMessage = "Transportation Type is required")]
        [StringLength(100, ErrorMessage = "Transportation Type cannot exceed 255 characters")]
        public string TransportationType { get; set; } = null!;

        public List<IFormFile>? TourImages { get; set; }
    }

    public class CreateTourDTO : BaseTourDTO
    {
    }

    public class UpdateTourDTO : BaseTourDTO
    {
        public bool RemoveImage { get; set; }
        public List<int>? RemovedTourImageIds { get; set; }
    }

    public class ReadUserDTO
    {
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public int Id { get; set; }
        public List<string> Roles { get; set; } = new List<string>();

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
