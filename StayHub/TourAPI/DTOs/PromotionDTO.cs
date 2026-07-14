using System.ComponentModel.DataAnnotations;

namespace TourAPI.DTOs
{
    public class ReadPromotionDTO
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = null!;
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public abstract class BasePromotionDTO
    {
        [Required(ErrorMessage = "Code is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Code must be between 3 and 50 characters")]
        public string Code { get; set; } = null!;

        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Name must be between 5 and 200 characters")]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        [Required(ErrorMessage = "DiscountType is required")]
        [RegularExpression("^(PERCENTAGE|FIXED)$", ErrorMessage = "DiscountType must be either 'PERCENTAGE' or 'FIXED'")]
        public string DiscountType { get; set; } = null!;

        [Required(ErrorMessage = "DiscountValue is required")]
        [Range(0, double.MaxValue, ErrorMessage = "DiscountValue must be a positive number")]
        public decimal DiscountValue { get; set; }

        public decimal? MaxDiscountAmount { get; set; }

        [Required(ErrorMessage = "StartDate is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "EndDate is required")]
        public DateTime EndDate { get; set; }

        public string? Status { get; set; } = "Active";
    }

    public class CreatePromotionDTO : BasePromotionDTO
    {
    }

    public class UpdatePromotionDTO : BasePromotionDTO
    {
    }

    public class ChangePromotionStatusDTO
    {
        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = null!;
    }
}
