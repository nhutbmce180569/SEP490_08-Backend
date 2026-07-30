using System;
using System.Collections.Generic;

namespace PaymentAPI.Models;

public partial class Transaction
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public long Amount { get; set; }

    public string Provider { get; set; } = null!;

    public string? ProviderTxnId { get; set; }

    public string? Status { get; set; }
}
