namespace BookingAPI.DTOs;

public class ApplyVoucherRequest
{
    public string Code { get; set; } = null!;

    public int? TourId { get; set; }

    public long BillAmount { get; set; }
}

public class ApplyVoucherResultDTO
{
    public bool IsValid { get; set; }

    public int VoucherId { get; set; }

    public string Code { get; set; } = null!;

    public long BillAmount { get; set; }

    public long DiscountAmount { get; set; }

    public long FinalAmount { get; set; }

    public string? Message { get; set; }
}

public class RestoreVoucherRequest
{
    public int CustomerId { get; set; }

    public string Code { get; set; } = null!;
}

public class VoucherApiErrorResponse
{
    public string? Message { get; set; }
}
