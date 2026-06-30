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
        public TimeOnly? StartDuration { get; set; }
        public TimeOnly? EndDuration { get; set; }
        public string? LocationName { get; set; }
        public double? LocationLat { get; set; }
        public double? LocationLng { get; set; }
        public int? TourismInfoId { get; set; }

    }

    public abstract class BaseTourItineraryDTO
    {
        [Required(ErrorMessage = "TourId is required")]
        public int TourId { get; set; }

        [Required(ErrorMessage = "DayNumber is required")]
        [Range(1, int.MaxValue, ErrorMessage = "DayNumber must be greater than 0")]
        public int DayNumber { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 255 characters")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "StartDuration is required")]
        public TimeOnly? StartDuration { get; set; }
        [Required(ErrorMessage = "EndDuration is required")]
        public TimeOnly? EndDuration { get; set; }

        [Required(ErrorMessage = "LocationName is required")]
        [StringLength(255, MinimumLength = 3, ErrorMessage = "LocationName must be between 3 and 255 characters")]
        public string? LocationName { get; set; }

        [Range(-90, 90, ErrorMessage = "LocationLat must be between -90 and 90")]
        public double? LocationLat { get; set; }
        [Range(-180, 180, ErrorMessage = "LocationLng must be between -180 and 180")]
        public double? LocationLng { get; set; }
        public int? TourismInfoId { get; set; }

    }

    public class CreateTourItineraryDTO : BaseTourItineraryDTO { }

    public class UpdateTourItineraryDTO : BaseTourItineraryDTO { }

    public class CreateTourItineraryBatchDTO
    {
        public List<CreateTourItineraryDTO> Itineraries { get; set; } = new List<CreateTourItineraryDTO>();
    }

}
