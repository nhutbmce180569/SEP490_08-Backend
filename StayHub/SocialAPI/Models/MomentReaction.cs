using System;
using System.Collections.Generic;

namespace SocialAPI.Models;

public partial class MomentReaction
{
    public int Id { get; set; }

    public int MomentId { get; set; }

    public int UserId { get; set; }

    public bool? IsLike { get; set; }

    public virtual TourMoment Moment { get; set; } = null!;
}
