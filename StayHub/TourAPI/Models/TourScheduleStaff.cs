using System;
using System.Collections.Generic;

namespace TourAPI.Models;

public partial class TourScheduleStaff
{
    public int Id { get; set; }

    public int ScheduleId { get; set; }

    public int StaffId { get; set; }

    public string? AssignedRole { get; set; }

    public virtual TourSchedule Schedule { get; set; } = null!;
}
