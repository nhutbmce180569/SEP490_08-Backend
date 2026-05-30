using System;
using System.ComponentModel.DataAnnotations;
using TourAPI.Models;

namespace TourAPI.DTOs
{
    public class ReadTourScheduleDTO
    {
        public int Id { get; set; }

        public int TourId { get; set; }

        public DateTime DepartureDate { get; set; }

        public DateTime ReturnDate { get; set; }

        public string? Note { get; set; }

        public virtual ReadTourBasicDTO? Tour { get; set; }

        public virtual ICollection<ReadTourScheduleStaffDTO>? TourScheduleStaffs { get; set; }

        public virtual ICollection<ReadTourScheduleItineraryDTO>? TourScheduleItineraries { get; set; }

        public virtual ICollection<ReadTourScheduleTicketDTO>? TourScheduleTickets { get; set; }
    }

    public abstract class BaseTourScheduleDTO
    {
        [Required(ErrorMessage = "TourId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "TourId must be greater than 0")]
        public int TourId { get; set; }

        [Required(ErrorMessage = "Departure date is required")]
        public DateTime DepartureDate { get; set; }

        [Required(ErrorMessage = "Return date is required")]
        public DateTime ReturnDate { get; set; }

        public string? Note { get; set; }
    }

    public class CreateTourScheduleDTO : BaseTourScheduleDTO { }

    public class UpdateTourScheduleDTO : BaseTourScheduleDTO { }
}
