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

        public long Price { get; set; }

        public int MaxCapacity { get; set; }

        public int SoldQuantity { get; set; }

        public int AvailableSeats { get; set; }

        public string? Note { get; set; }

        public virtual ReadTourBasicDTO? Tour { get; set; }

        public virtual ICollection<ReadTourScheduleStaffDTO>? TourScheduleStaffs { get; set; }

        public virtual ICollection<ReadTourScheduleItineraryDTO>? TourScheduleItineraries { get; set; }

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

        [Required(ErrorMessage = "Max capacity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "AvailableSlots cannot be negative")]
        public int MaxCapacity { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, long.MaxValue, ErrorMessage = "Price cannot be negative")]
        public long Price { get; set; }

        [RegularExpression("^(Active|Inactive|Cancelled)$", ErrorMessage = "Status must be Active, Inactive, or Cancelled")]
        public string? Status { get; set; }

        public string? Note { get; set; }

        [Required(ErrorMessage = "Available seats is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Available seat cannot be negative")]
        public int AvailableSeats { get; set; }

    }

    public class CreateTourScheduleDTO : BaseTourScheduleDTO { }

    public class UpdateTourScheduleDTO : BaseTourScheduleDTO { }

    public class ReserveScheduleSeatsDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }

    public class ReleaseScheduleSeatsDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }
}