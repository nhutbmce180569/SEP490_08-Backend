using System;
using System.Collections.Generic;

namespace ContentAPI.Models;

public partial class TicketType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? MinAge { get; set; }
    
    public int? MaxAge { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
