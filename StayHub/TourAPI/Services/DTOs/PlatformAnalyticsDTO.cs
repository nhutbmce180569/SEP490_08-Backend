namespace VoucherAPI.DTOs;

public class PlatformVoucherStatsDTO
{
    public int TotalVouchers { get; set; }
    public int ActiveVouchers { get; set; }
    public int InactiveVouchers { get; set; }
    public int ExpiredVouchers { get; set; }
    public int TotalRedemptions { get; set; }
    public int TotalUserVoucherAssignments { get; set; }
    public int UsedUserVouchers { get; set; }
    public int AvailableUserVouchers { get; set; }
    public int ExpiredUserVouchers { get; set; }
    public decimal RedemptionRate { get; set; }
    public List<LabelCountDTO> ByDiscountType { get; set; } = [];
    public List<LabelCountDTO> ByUserVoucherStatus { get; set; } = [];
}

public class LabelCountDTO
{
    public string Label { get; set; } = null!;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}
