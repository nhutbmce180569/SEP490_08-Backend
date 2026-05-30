using Microsoft.EntityFrameworkCore;
using VoucherAPI.Models;

namespace VoucherAPI.Repositories.Implements;

public class UserVoucherRepository : IUserVoucherRepository
{
    private readonly StayHubVoucherDbContext _context;

    public UserVoucherRepository(StayHubVoucherDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserVoucher>> GetByVoucherIdAsync(int voucherId)
    {
        return await _context.UserVouchers
            .Where(uv => uv.VoucherId == voucherId)
            .OrderBy(uv => uv.Id)
            .ToListAsync();
    }

    public async Task<bool> ExistsForUserAsync(int voucherId, int userId)
    {
        return await _context.UserVouchers.AnyAsync(uv =>
            uv.VoucherId == voucherId && uv.UserId == userId);
    }

    public async Task AddRangeAsync(IEnumerable<UserVoucher> entities)
    {
        await _context.UserVouchers.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }
}
