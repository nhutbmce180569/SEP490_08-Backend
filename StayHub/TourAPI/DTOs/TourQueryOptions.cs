namespace TourAPI.DTOs
{
    public class TourQueryOptions
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public bool ActiveOnly { get; set; }
        public bool SortDescendingById { get; set; }
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public long? MinPrice { get; set; }
        public long? MaxPrice { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Duration { get; set; }
        public string? SortBy { get; set; }
        public int? CreatedBy { get; set; }
    }
}
