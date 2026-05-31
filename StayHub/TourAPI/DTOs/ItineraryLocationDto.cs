using System;

namespace TourAPI.DTOs
{
    public class ItineraryLocationDto
    {
        public int Id { get; set; }
        public int DayNumber { get; set; }
        public string Title { get; set; } = null!;
        public string LocationName { get; set; } = null!;
        public double? LocationLat { get; set; }
        public double? LocationLng { get; set; }
        public TimeSpan? StartDuration { get; set; } 
        public TimeSpan? EndDuration { get; set; }
    }
}
