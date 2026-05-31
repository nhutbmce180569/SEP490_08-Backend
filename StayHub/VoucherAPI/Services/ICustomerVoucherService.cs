using VoucherAPI.DTOs;

namespace VoucherAPI.Services;

public interface ICustomerVoucherService
{
    Task<ReadSavedVoucherDTO> SaveVoucherAsync(int userId, string code);

    Task<PaginationDTO<ReadSavedVoucherDTO>> GetMySavedVouchersAsync(
        int userId,
        int page,
        int pageSize,
        string? status);

    Task<ApplyVoucherResultDTO> ApplyVoucherAsync(int userId, ApplyVoucherDTO dto);

    Task<ApplyVoucherResultDTO> RedeemVoucherAsync(int userId, ApplyVoucherDTO dto);

    Task RestoreVoucherAsync(int userId, string code);
}
