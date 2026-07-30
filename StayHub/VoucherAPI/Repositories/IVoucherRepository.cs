using VoucherAPI.Models;

namespace VoucherAPI.Repositories;

public interface IVoucherRepository
{
    Task<IQueryable<Voucher>> GetQueryableAsync();
    Task<IEnumerable<Voucher>> GetAllAsync();

    Task<Voucher?> GetByIdAsync(int id);

    Task<Voucher?> GetByIdWithUserVouchersAsync(int id);

    Task<Voucher?> GetByCodeAsync(string code);

    Task<Voucher?> GetByCodeWithUserVouchersAsync(string code);

    Task<bool> CodeExistsAsync(string code, int? exceptId = null);

    Task AddAsync(Voucher entity);

    Task AddWithAssignmentsAsync(Voucher entity, IEnumerable<UserVoucher> userVouchers);

    Task UpdateAsync(Voucher entity);

    Task SetActiveAsync(Voucher entity, bool isActive);

    Task RedeemAsync(int voucherId, int userVoucherId);

    Task RestoreAsync(int voucherId, int userVoucherId);
}
