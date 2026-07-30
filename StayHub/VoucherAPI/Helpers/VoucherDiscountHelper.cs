using VoucherAPI.Models;

namespace VoucherAPI.Helpers;

public static class VoucherDiscountHelper
{
    /// <summary>
    /// Tính số tiền giảm thực tế khi áp dụng voucher lên bill.
    /// Percent: min(bill * %, MaxDiscountAmount); Amount: min(DiscountValue, bill).
    /// </summary>
    public static long CalculateDiscountAmount(Voucher voucher, long billAmount)
    {
        if (billAmount <= 0)
        {
            return 0;
        }

        if (voucher.MinOrderAmount.HasValue && billAmount < voucher.MinOrderAmount.Value)
        {
            return 0;
        }

        if (voucher.DiscountType.Equals("Percent", StringComparison.OrdinalIgnoreCase))
        {
            var percentDiscount = billAmount * voucher.DiscountValue / 100;

            if (voucher.MaxDiscountAmount.HasValue && percentDiscount > voucher.MaxDiscountAmount.Value)
            {
                return voucher.MaxDiscountAmount.Value;
            }

            return percentDiscount;
        }

        if (voucher.DiscountType.Equals("Amount", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Min(voucher.DiscountValue, billAmount);
        }

        return 0;
    }
}
