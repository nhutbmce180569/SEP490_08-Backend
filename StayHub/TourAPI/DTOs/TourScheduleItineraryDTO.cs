using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TourAPI.DTOs
{
    public class ReadTourScheduleItineraryDTO
    {
        public int Id { get; set; }
        public int ScheduleId { get; set; }
        public DateTime ItineraryDate { get; set; }
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

    public abstract class BaseTourScheduleItineraryDTO
    {
        [Required(ErrorMessage = "ScheduleId is required")]
        public int ScheduleId { get; set; }

        [Required(ErrorMessage = "ItineraryDate is required")]
        public DateTime ItineraryDate { get; set; }

        [Required(ErrorMessage = "DayNumber is required")]
        [Range(1, int.MaxValue, ErrorMessage = "DayNumber must be greater than 0")]
        public int DayNumber { get; set; }

        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "StartDuration is required")]
        public TimeOnly? StartDuration { get; set; }

        [Required(ErrorMessage = "EndDuration is required")]
        public TimeOnly? EndDuration { get; set; }

        [StringLength(255, ErrorMessage = "LocationName cannot exceed 255 characters")]
        public string? LocationName { get; set; }

        public double? LocationLat { get; set; }

        public double? LocationLng { get; set; }
        public int? TourismInfoId { get; set; }

    }

    public class CreateTourScheduleItineraryDTO : BaseTourScheduleItineraryDTO { }

    public class UpdateTourScheduleItineraryDTO : BaseTourScheduleItineraryDTO { }

    public class CreateTourScheduleItineraryBatchDTO
    {
        public List<CreateTourScheduleItineraryDTO> Itineraries { get; set; } = new List<CreateTourScheduleItineraryDTO>();
    }
}
