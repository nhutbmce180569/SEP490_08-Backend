using System;

namespace BookingAPI.DTOs
{
    // DTO trả về cho Frontend
    public class EligibleScheduleDto
    {
        public int ScheduleId { get; set; }
        public string TourName { get; set; } = null!;
        public DateTime DepartureDate { get; set; }
        public DateTime ReturnDate { get; set; }
        public string StatusContext { get; set; } = null!; // "Ongoing", "Upcoming", "Completed"
    }

    // DTO dùng để hứng dữ liệu trả về khi gọi sang CatalogAPI (TourAPI)
    public class TourScheduleDetailDto
    {
        public int Id { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime ReturnDate { get; set; }
        public TourBasicDto? Tour { get; set; }
    }

    public class TourBasicDto
    {
        public string Name { get; set; } = null!;
    }
}
