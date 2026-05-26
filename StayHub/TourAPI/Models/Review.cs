using System;
using System.Collections.Generic;

namespace TourAPI.Models;

public partial class Review
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int TourId { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ReviewReply> ReviewReplies { get; set; } = new List<ReviewReply>();

    public virtual Tour Tour { get; set; } = null!;
}
