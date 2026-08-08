using VoucherAPI.Models;

namespace VoucherAPI.Repositories;

public interface IUserVoucherRepository
{
    Task<IEnumerable<UserVoucher>> GetByVoucherIdAsync(int voucherId);

    Task<List<UserVoucher>> GetByUserIdAsync(int userId);

    Task<UserVoucher?> GetByUserAndVoucherAsync(int userId, int voucherId);

    Task<bool> ExistsForUserAsync(int voucherId, int userId);

    Task AddAsync(UserVoucher entity);

    Task AddRangeAsync(IEnumerable<UserVoucher> entities);

    Task UpdateAsync(UserVoucher entity);

    Task DeleteRangeAsync(IEnumerable<UserVoucher> entities);
}
