using TourAPI.Models;

namespace TourAPI.DTOs
{
    public class TourPageResult
    {
        public List<Tour> Items { get; set; } = new();
        public int Total { get; set; }
    }
}
