using System;

namespace TourAPI.Models;

public partial class TourImage
{
    public int Id { get; set; }

    public int TourId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Tour Tour { get; set; } = null!;
}
