using System;
using System.Collections.Generic;

namespace BookingAPI.Models;

public partial class CancellationRequest
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public string BankName { get; set; } = null!;

    public string AccountNumber { get; set; } = null!;

    public string AccountHolderName { get; set; } = null!;

    public DateTime? RequestedAt { get; set; }

    public long OriginalAmount { get; set; }

    public long CancellationFee { get; set; }

    public int FeePercent { get; set; }

    public long RefundAmount { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? RejectReason { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int? ProcessedBy { get; set; }

    public virtual Order Order { get; set; } = null!;
}
