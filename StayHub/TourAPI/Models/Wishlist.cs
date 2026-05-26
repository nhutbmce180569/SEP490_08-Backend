using System;
using System.Collections.Generic;

namespace TourAPI.Models;

public partial class Wishlist
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int TourId { get; set; }

    public virtual Tour Tour { get; set; } = null!;
}
