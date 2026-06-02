namespace TourAPI.DTOs;

public class AssignStaffRequestDto
{
    public int ScheduleId { get; set; }

    public int StaffId { get; set; }

    public string? AssignedRole { get; set; }
}
