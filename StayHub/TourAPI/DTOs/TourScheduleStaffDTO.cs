using System.ComponentModel.DataAnnotations;

namespace TourAPI.DTOs
{
    public class ReadTourScheduleStaffDTO
    {
        public int Id { get; set; }

        public int ScheduleId { get; set; }

        public int StaffId { get; set; }

        public string? AssignedRole { get; set; }
    }

    public abstract class BaseTourScheduleStaffDTO
    {
        [Required(ErrorMessage = "ScheduleId is required")]
        public int ScheduleId { get; set; }

        [Required(ErrorMessage = "StaffId is required")]
        public int StaffId { get; set; }

        [StringLength(255, ErrorMessage = "AssignedRole cannot exceed 255 characters")]
        public string? AssignedRole { get; set; }
    }

    public class CreateTourScheduleStaffDTO : BaseTourScheduleStaffDTO { }

    public class UpdateTourScheduleStaffDTO : BaseTourScheduleStaffDTO { }
}
