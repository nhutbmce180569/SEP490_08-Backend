using Microsoft.EntityFrameworkCore;
using VoucherAPI.Models;

namespace VoucherAPI.Repositories.Implements;

public class VoucherRepository : IVoucherRepository
{
    private readonly StayHubVoucherDbContext _context;

    public VoucherRepository(StayHubVoucherDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Voucher>> GetAllAsync()
    {
        return await _context.Vouchers
            .Include(v => v.UserVouchers)
            .OrderByDescending(v => v.Id)
            .ToListAsync();
    }

    public async Task<Voucher?> GetByIdAsync(int id)
    {
        return await _context.Vouchers.FindAsync(id);
    }

    public async Task<Voucher?> GetByIdWithUserVouchersAsync(int id)
    {
        return await _context.Vouchers
            .Include(v => v.UserVouchers)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Voucher?> GetByCodeAsync(string code)
    {
        return await _context.Vouchers
            .FirstOrDefaultAsync(v => v.Code == code);
    }

    public async Task<bool> CodeExistsAsync(string code, int? exceptId = null)
    {
        return await _context.Vouchers.AnyAsync(v =>
            v.Code == code && (exceptId == null || v.Id != exceptId));
    }

    public async Task AddAsync(Voucher entity)
    {
        await _context.Vouchers.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddWithAssignmentsAsync(Voucher entity, IEnumerable<UserVoucher> userVouchers)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.Vouchers.AddAsync(entity);
            await _context.SaveChangesAsync();

            var assignments = userVouchers.ToList();
            if (assignments.Count > 0)
            {
                foreach (var assignment in assignments)
                {
                    assignment.VoucherId = entity.Id;
                }

                await _context.UserVouchers.AddRangeAsync(assignments);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateAsync(Voucher entity)
    {
        _context.Vouchers.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task SetActiveAsync(Voucher entity, bool isActive)
    {
        entity.IsActive = isActive;
        _context.Vouchers.Update(entity);
        await _context.SaveChangesAsync();
    }
}
