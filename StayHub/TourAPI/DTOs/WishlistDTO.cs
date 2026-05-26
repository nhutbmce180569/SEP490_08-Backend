using System.ComponentModel.DataAnnotations;

namespace TourAPI.DTOs
{
    public class ReadWishlistItemDTO
    {
        public int WishlistId { get; set; }
        public int TourId { get; set; }
        public string TourName { get; set; } = null!;
        public string? TourImageUrl { get; set; }
        public string? TourStatus { get; set; }
        public string? TourDescription { get; set; }
    }

    public class WishlistActionRequestDTO
    {
        [Range(1, int.MaxValue)]
        public int TourId { get; set; }

        [Required]
        [RegularExpression("^(add|rem)$", ErrorMessage = "Action must be add or rem.")]
        public string Action { get; set; } = null!;
    }
}
