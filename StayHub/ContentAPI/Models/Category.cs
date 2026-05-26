using System;
using System.Collections.Generic;

namespace ContentAPI.Models;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? IconUrl { get; set; }

    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}
