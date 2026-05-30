using System;
using System.Collections.Generic;

namespace BookingAPI.Models;

public partial class OrderDetail
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int TicketTypeId { get; set; }

    public int TourScheduleTicketId { get; set; }

    public int Quantity { get; set; }

    public long UnitPrice { get; set; }

    public long TotalPrice { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
