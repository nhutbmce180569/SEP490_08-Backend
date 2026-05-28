using System;
using System.Collections.Generic;

namespace BookingAPI.Models;

public partial class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int ScheduleId { get; set; }

    public int TicketCount { get; set; }

    public long? DiscountValue { get; set; }

    public long FinalAmount { get; set; }

    public string? Note { get; set; }

    public string? Status { get; set; }

    public DateTime? OrderedAt { get; set; }

    public string? InviteToken { get; set; }

    public virtual ICollection<CancellationRequest> CancellationRequests { get; set; } = new List<CancellationRequest>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
