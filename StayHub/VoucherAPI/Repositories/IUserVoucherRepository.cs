using VoucherAPI.Models;

namespace VoucherAPI.Repositories;

public interface IUserVoucherRepository
{
    Task<IEnumerable<UserVoucher>> GetByVoucherIdAsync(int voucherId);

    Task<bool> ExistsForUserAsync(int voucherId, int userId);

    Task AddRangeAsync(IEnumerable<UserVoucher> entities);
}
