using System;
using System.Collections.Generic;

namespace SocialAPI.Models;

public partial class TourMoment
{
    public int Id { get; set; }

    public int ScheduleId { get; set; }

    public int UserId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? Caption { get; set; }

    public double? Lat { get; set; }

    public double? Lng { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Privacy { get; set; }

    public virtual ICollection<MomentComment> MomentComments { get; set; } = new List<MomentComment>();

    public virtual ICollection<MomentReaction> MomentReactions { get; set; } = new List<MomentReaction>();
}
