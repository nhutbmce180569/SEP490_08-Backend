using Microsoft.EntityFrameworkCore;
using VoucherAPI.DTOs;
using VoucherAPI.Models;

namespace VoucherAPI.Services.Implements;

public class PlatformAnalyticsService : IPlatformAnalyticsService
{
    private readonly StayHubVoucherDbContext _context;

    public PlatformAnalyticsService(StayHubVoucherDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformVoucherStatsDTO> GetVoucherStatsAsync()
    {
        var now = DateTime.UtcNow;
        var vouchers = await _context.Vouchers.AsNoTracking().ToListAsync();
        var userVouchers = await _context.UserVouchers.AsNoTracking().ToListAsync();

        var total = vouchers.Count;
        var active = vouchers.Count(v => v.IsActive && v.EndDate >= now && v.StartDate <= now);
        var inactive = vouchers.Count(v => !v.IsActive);
        var expired = vouchers.Count(v => v.EndDate < now);

        var totalRedemptions = vouchers.Sum(v => v.UsedCount);
        var uvTotal = userVouchers.Count;

        return new PlatformVoucherStatsDTO
        {
            TotalVouchers = total,
            ActiveVouchers = active,
            InactiveVouchers = inactive,
            ExpiredVouchers = expired,
            TotalRedemptions = totalRedemptions,
            TotalUserVoucherAssignments = uvTotal,
            UsedUserVouchers = userVouchers.Count(uv => uv.Status == "Used"),
            AvailableUserVouchers = userVouchers.Count(uv => uv.Status == "Available"),
            ExpiredUserVouchers = userVouchers.Count(uv => uv.Status == "Expired"),
            RedemptionRate = uvTotal > 0
                ? Math.Round(userVouchers.Count(uv => uv.Status == "Used") * 100m / uvTotal, 2)
                : 0,
            ByDiscountType = vouchers.Count > 0
                ? vouchers
                    .GroupBy(v => v.DiscountType ?? "Unknown")
                    .Select(g => new LabelCountDTO
                    {
                        Label = g.Key,
                        Count = g.Count(),
                        Percentage = Math.Round(g.Count() * 100m / total, 2)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList()
                : [],
            ByUserVoucherStatus = uvTotal > 0
                ? userVouchers
                    .GroupBy(uv => uv.Status ?? "Unknown")
                    .Select(g => new LabelCountDTO
                    {
                        Label = g.Key,
                        Count = g.Count(),
                        Percentage = Math.Round(g.Count() * 100m / uvTotal, 2)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList()
                : []
        };
    }
}
