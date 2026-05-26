using System;
using System.Collections.Generic;

namespace ContentAPI.Models;

public partial class Banner
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string ImageUrl { get; set; } = null!;

    public string? TargetUrl { get; set; }

    public int? Priority { get; set; }

    public bool? IsActive { get; set; }
}
