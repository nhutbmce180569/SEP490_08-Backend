namespace VoucherAPI.Models;

public partial class UserVoucher
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int VoucherId { get; set; }

    public int Quantity { get; set; }

    public string Status { get; set; } = "Available";

    public virtual Voucher Voucher { get; set; } = null!;
}
