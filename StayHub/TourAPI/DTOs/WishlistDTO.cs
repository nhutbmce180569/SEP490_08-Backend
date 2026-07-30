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
}
