using System;
using System.Collections.Generic;

namespace SocialAPI.Models;

public partial class Sosalert
{
    public int Id { get; set; }

    public int SenderId { get; set; }

    public int? ScheduleId { get; set; }

    public double Lat { get; set; }

    public double Lng { get; set; }

    public string? Message { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }
}
