using System;
using System.Collections.Generic;

namespace TourAPI.Models;

public partial class Tour
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Country { get; set; }

    public string? City { get; set; }

    public string? Address { get; set; }

    public string? ImageUrl { get; set; }

    public string SourceName { get; set; } = "Vietnam National Administration of Tourism";

    public string? SourceUrl { get; set; }

    public int CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<TourItinerary> TourItineraries { get; set; } = new List<TourItinerary>();

    public virtual ICollection<TourSchedule> TourSchedules { get; set; } = new List<TourSchedule>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
