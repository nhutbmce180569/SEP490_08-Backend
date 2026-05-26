namespace TourAPI.DTOs
{
    public class PaginationDTO<T>
    {
        public List<T>? Data { get; set; }

        public int Total { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
