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

    public async Task<List<UserVoucher>> GetByUserIdAsync(int userId)
    {
        return await _context.UserVouchers
            .Include(uv => uv.Voucher)
            .Where(uv => uv.UserId == userId)
            .OrderByDescending(uv => uv.Id)
            .ToListAsync();
    }

    public async Task<UserVoucher?> GetByUserAndVoucherAsync(int userId, int voucherId)
    {
        return await _context.UserVouchers
            .Include(uv => uv.Voucher)
            .FirstOrDefaultAsync(uv => uv.UserId == userId && uv.VoucherId == voucherId);
    }

    public async Task<bool> ExistsForUserAsync(int voucherId, int userId)
    {
        return await _context.UserVouchers.AnyAsync(uv =>
            uv.VoucherId == voucherId && uv.UserId == userId);
    }

    public async Task AddAsync(UserVoucher entity)
    {
        await _context.UserVouchers.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<UserVoucher> entities)
    {
        await _context.UserVouchers.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserVoucher entity)
    {
        _context.UserVouchers.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(UserVoucher entity)
    {
        _context.UserVouchers.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<UserVoucher> entities)
    {
        _context.UserVouchers.RemoveRange(entities);
        await _context.SaveChangesAsync();
    }
}
