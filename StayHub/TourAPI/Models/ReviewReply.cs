using System;
using System.Collections.Generic;

namespace TourAPI.Models;

public partial class ReviewReply
{
    public int Id { get; set; }

    public int ReviewId { get; set; }

    public int UserId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Review Review { get; set; } = null!;
}
