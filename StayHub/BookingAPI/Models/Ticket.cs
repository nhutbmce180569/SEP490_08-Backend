using System;
using System.Collections.Generic;

namespace BookingAPI.Models;

public partial class Ticket
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int? UserId { get; set; }

    public string AttendeeName { get; set; } = null!;

    public string IdCard { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Nationality { get; set; }

    public string? QrCode { get; set; }

    public string? CheckInStatus { get; set; }

    public virtual Order Order { get; set; } = null!;
}
