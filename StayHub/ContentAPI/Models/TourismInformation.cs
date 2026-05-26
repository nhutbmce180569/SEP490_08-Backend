using System;
using System.Collections.Generic;

namespace ContentAPI.Models;

public partial class TourismInformation
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Description { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? Country { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? ImageUrl { get; set; }

    public string? SourceName { get; set; }

    public string? SourceUrl { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
