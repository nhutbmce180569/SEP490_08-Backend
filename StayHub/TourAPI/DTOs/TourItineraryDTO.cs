using System.ComponentModel.DataAnnotations;

namespace TourAPI.DTOs
{
    public class ReadTourItineraryDTO
    {
        public int Id { get; set; }
        public int TourId { get; set; }
        public int DayNumber { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        [Required(ErrorMessage = "StartDuration is required")]
        public TimeOnly? StartDuration { get; set; }

        [Required(ErrorMessage = "EndDuration is required")]
        public TimeOnly? EndDuration { get; set; }
        public string? LocationName { get; set; }
        public double? LocationLat { get; set; }
        public double? LocationLng { get; set; }
    }

    public abstract class BaseTourItineraryDTO
    {
        [Required(ErrorMessage = "TourId is required")]
        public int TourId { get; set; }

        [Required(ErrorMessage = "DayNumber is required")]
        [Range(1, int.MaxValue, ErrorMessage = "DayNumber must be greater than 0")]
        public int DayNumber { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string? Description { get; set; }

        public TimeOnly? StartDuration { get; set; }
        public TimeOnly? EndDuration { get; set; }

        [StringLength(255, ErrorMessage = "LocationName cannot exceed 255 characters")]
        public string? LocationName { get; set; }

        public double? LocationLat { get; set; }
        public double? LocationLng { get; set; }
    }

    public class CreateTourItineraryDTO : BaseTourItineraryDTO { }

    public class UpdateTourItineraryDTO : BaseTourItineraryDTO { }

    public class CreateTourItineraryBatchDTO
    {
        public List<CreateTourItineraryDTO> Itineraries { get; set; } = new List<CreateTourItineraryDTO>();
    }

}
