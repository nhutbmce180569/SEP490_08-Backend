namespace TourAPI.DTOs
{
    public class ScheduleStaffDetailDto
    {
        public int StaffId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string AssignedRole { get; set; } = string.Empty;
    }
}