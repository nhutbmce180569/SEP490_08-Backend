using System.ComponentModel.DataAnnotations;

namespace VoucherAPI.DTOs;

public class SaveVoucherDTO
{
    [Required(ErrorMessage = "Code is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Code must be between 3 and 50 characters")]
    public string Code { get; set; } = null!;
}

public class ApplyVoucherDTO
{
    [Required(ErrorMessage = "Code is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Code must be between 3 and 50 characters")]
    public string Code { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "TourId must be greater than 0 when provided")]
    public int? TourId { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "BillAmount must be greater than 0")]
    public long BillAmount { get; set; }
}

public class ReadSavedVoucherDTO
{
    public int UserVoucherId { get; set; }

    public int VoucherId { get; set; }

    public string Code { get; set; } = null!;

    public int? TourId { get; set; }

    public string? TourName { get; set; }

    public string DiscountType { get; set; } = null!;

    public long DiscountValue { get; set; }

    public long? MaxDiscountAmount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? Description { get; set; }

    public int Quantity { get; set; }

    public string Status { get; set; } = null!;

    public string VoucherStatus { get; set; } = null!;

    public bool IsActive { get; set; }
}

public class RestoreVoucherDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "CustomerId must be greater than 0")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Code is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Code must be between 3 and 50 characters")]
    public string Code { get; set; } = null!;
}

public class ApplyVoucherResultDTO
{
    public bool IsValid { get; set; }

    public int VoucherId { get; set; }

    public string Code { get; set; } = null!;

    public string DiscountType { get; set; } = null!;

    public long DiscountValue { get; set; }

    public long? MaxDiscountAmount { get; set; }

    public long BillAmount { get; set; }

    public long DiscountAmount { get; set; }

    public long FinalAmount { get; set; }

    public string? Message { get; set; }
}
