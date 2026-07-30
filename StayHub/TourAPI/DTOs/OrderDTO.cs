namespace TourAPI.DTOs
{
    public class CheckCompletedBookingRequest
    {
        public int CustomerId { get; set; }
        public List<int> ScheduleIds { get; set; } = new List<int>();
    }
    public class CheckBookingTour
    {
        public List<int> ScheduleIds { get; set; } = new List<int>();
    }
}
