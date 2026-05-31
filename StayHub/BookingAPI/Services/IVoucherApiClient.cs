using BookingAPI.DTOs;

namespace BookingAPI.Services;

public interface IVoucherApiClient
{
    Task<ApplyVoucherResultDTO> RedeemVoucherAsync(ApplyVoucherRequest request);

    Task RestoreVoucherAsync(int customerId, string voucherCode);
}
