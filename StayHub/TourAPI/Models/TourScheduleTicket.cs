using System;
using System.Collections.Generic;

namespace TourAPI.Models;

public partial class TourScheduleTicket
{
    public int Id { get; set; }

    public int ScheduleId { get; set; }

    public int TicketTypeId { get; set; }

    public long Price { get; set; }

    public int Quantity { get; set; }

    public int? SoldQuantity { get; set; }

    public int AvailableQuantity { get; set; }

    public bool? IsActive { get; set; }

    public string? Note { get; set; }

    public virtual TourSchedule Schedule { get; set; } = null!;

    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();
}
