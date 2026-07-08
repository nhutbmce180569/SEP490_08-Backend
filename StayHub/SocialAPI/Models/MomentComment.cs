using System;
using System.Collections.Generic;

namespace SocialAPI.Models;

public partial class MomentComment
{
    public int Id { get; set; }

    public int MomentId { get; set; }

    public int UserId { get; set; }

    public string Comment { get; set; } = null!;

    public DateTime? Timestamp { get; set; }

    public string Status { get; set; } = "Approved";

    public virtual TourMoment Moment { get; set; } = null!;
}
