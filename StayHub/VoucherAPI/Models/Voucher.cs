namespace VoucherAPI.Models;

public partial class Voucher
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public int? TourId { get; set; }

    public string DiscountType { get; set; } = null!;

    public long DiscountValue { get; set; }

    /// <summary>Trần tiền giảm tối đa — chỉ áp dụng khi DiscountType = Percent.</summary>
    public long? MaxDiscountAmount { get; set; }

    /// <summary>Giá trị đơn hàng tối thiểu để áp dụng voucher.</summary>
    public long? MinOrderAmount { get; set; }

    public int UsedCount { get; set; }

    public int AvailableCount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? Description { get; set; }

    public int CreatorId { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();
}
