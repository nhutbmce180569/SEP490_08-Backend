using System;
using System.Collections.Generic;

namespace SocialAPI.Models;

public partial class LocationLog
{
    public long Id { get; set; }

    public int ScheduleId { get; set; }

    public int UserId { get; set; }

    public double Lat { get; set; }

    public double Lng { get; set; }

    public DateTime? Timestamp { get; set; }
}
