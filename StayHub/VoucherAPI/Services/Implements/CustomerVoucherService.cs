using AutoMapper;
using VoucherAPI.DTOs;
using VoucherAPI.Helpers;
using VoucherAPI.Models;
using VoucherAPI.Repositories;

namespace VoucherAPI.Services.Implements;

public class CustomerVoucherService : ICustomerVoucherService
{
    private readonly IVoucherRepository _voucherRepository;
    private readonly IUserVoucherRepository _userVoucherRepository;
    private readonly ITourValidationService _tourValidationService;
    private readonly IMapper _mapper;

    public CustomerVoucherService(
        IVoucherRepository voucherRepository,
        IUserVoucherRepository userVoucherRepository,
        ITourValidationService tourValidationService,
        IMapper mapper)
    {
        _voucherRepository = voucherRepository;
        _userVoucherRepository = userVoucherRepository;
        _tourValidationService = tourValidationService;
        _mapper = mapper;
    }

    public async Task<ReadSavedVoucherDTO> SaveVoucherAsync(int userId, string code)
    {
        var normalizedCode = NormalizeCode(code);
        var voucher = await _voucherRepository.GetByCodeWithUserVouchersAsync(normalizedCode);
        if (voucher == null)
        {
            throw new Exception($"Voucher code '{normalizedCode}' not found");
        }

        ValidateVoucherEligibility(voucher);

        if (voucher.UserVouchers.Any())
        {
            throw new Exception("This voucher is exclusive to selected customers and cannot be saved manually");
        }

        if (await _userVoucherRepository.ExistsForUserAsync(voucher.Id, userId))
        {
            throw new Exception("You have already saved this voucher");
        }

        var userVoucher = new UserVoucher
        {
            UserId = userId,
            VoucherId = voucher.Id,
            Quantity = 1,
            Status = "Available"
        };

        await _userVoucherRepository.AddAsync(userVoucher);

        userVoucher.Voucher = voucher;
        return await MapSavedVoucherAsync(userVoucher);
    }

    public async Task<PaginationDTO<ReadSavedVoucherDTO>> GetMySavedVouchersAsync(
        int userId,
        int page,
        int pageSize,
        string? status)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var userVouchers = await _userVoucherRepository.GetByUserIdAsync(userId);
        var items = new List<ReadSavedVoucherDTO>();

        foreach (var userVoucher in userVouchers)
        {
            await SyncExpiredStatusAsync(userVoucher);
            items.Add(await MapSavedVoucherAsync(userVoucher));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var filter = status.Trim();
            items = items.Where(v =>
                v.Status.Equals(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var total = items.Count;
        var paged = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginationDTO<ReadSavedVoucherDTO>
        {
            Data = paged,
            Total = total,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize),
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    public async Task<ApplyVoucherResultDTO> ApplyVoucherAsync(int userId, ApplyVoucherDTO dto)
    {
        var (voucher, _, discountAmount) = await ValidateForUseAsync(userId, dto);
        return BuildApplyResult(voucher, dto.BillAmount, discountAmount, "Voucher applied successfully");
    }

    public async Task<ApplyVoucherResultDTO> RedeemVoucherAsync(int userId, ApplyVoucherDTO dto)
    {
        var (voucher, userVoucher, discountAmount) = await ValidateForUseAsync(userId, dto);
        await _voucherRepository.RedeemAsync(voucher.Id, userVoucher.Id);
        return BuildApplyResult(voucher, dto.BillAmount, discountAmount, "Voucher redeemed successfully");
    }

    public async Task RestoreVoucherAsync(int userId, string code)
    {
        var normalizedCode = NormalizeCode(code);
        var voucher = await _voucherRepository.GetByCodeAsync(normalizedCode);
        if (voucher == null)
        {
            throw new Exception($"Voucher code '{normalizedCode}' not found");
        }

        var userVoucher = await _userVoucherRepository.GetByUserAndVoucherAsync(userId, voucher.Id);
        if (userVoucher == null)
        {
            throw new Exception("User voucher not found");
        }

        if (voucher.UsedCount <= 0)
        {
            throw new Exception("Voucher has not been redeemed yet");
        }

        await _voucherRepository.RestoreAsync(voucher.Id, userVoucher.Id);
    }

    private async Task<(Voucher Voucher, UserVoucher UserVoucher, long DiscountAmount)> ValidateForUseAsync(
        int userId,
        ApplyVoucherDTO dto)
    {
        var normalizedCode = NormalizeCode(dto.Code);
        var voucher = await _voucherRepository.GetByCodeWithUserVouchersAsync(normalizedCode);
        if (voucher == null)
        {
            throw new Exception($"Voucher code '{normalizedCode}' not found");
        }

        ValidateVoucherEligibility(voucher);
        await ValidateTourForApplyAsync(voucher, dto.TourId);

        var userVoucher = await _userVoucherRepository.GetByUserAndVoucherAsync(userId, voucher.Id);
        if (userVoucher == null)
        {
            throw new Exception("Please save this voucher to your account before applying");
        }

        await SyncExpiredStatusAsync(userVoucher);

        if (!userVoucher.Status.Equals("Available", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"This voucher is no longer available (status: {userVoucher.Status})");
        }

        if (userVoucher.Quantity <= 0)
        {
            throw new Exception("You have no remaining uses for this voucher");
        }

        if (voucher.MinOrderAmount.HasValue && dto.BillAmount < voucher.MinOrderAmount.Value)
        {
            throw new Exception($"Đơn hàng tối thiểu để áp dụng voucher này là {voucher.MinOrderAmount.Value:N0} VNĐ");
        }

        var discountAmount = VoucherDiscountHelper.CalculateDiscountAmount(voucher, dto.BillAmount);
        if (discountAmount <= 0)
        {
            throw new Exception("This voucher cannot be applied to the current bill amount");
        }

        return (voucher, userVoucher, discountAmount);
    }

    private static ApplyVoucherResultDTO BuildApplyResult(
        Voucher voucher,
        long billAmount,
        long discountAmount,
        string message)
    {
        return new ApplyVoucherResultDTO
        {
            IsValid = true,
            VoucherId = voucher.Id,
            Code = voucher.Code,
            DiscountType = voucher.DiscountType,
            DiscountValue = voucher.DiscountValue,
            MaxDiscountAmount = voucher.MaxDiscountAmount,
            MinOrderAmount = voucher.MinOrderAmount,
            BillAmount = billAmount,
            DiscountAmount = discountAmount,
            FinalAmount = billAmount - discountAmount,
            Message = message
        };
    }

    private async Task<ReadSavedVoucherDTO> MapSavedVoucherAsync(UserVoucher userVoucher)
    {
        var dto = _mapper.Map<ReadSavedVoucherDTO>(userVoucher);

        if (userVoucher.Voucher.TourId.HasValue)
        {
            var tour = await _tourValidationService.ValidateTourAsync(userVoucher.Voucher.TourId.Value);
            dto.TourName = tour.Name;
        }

        return dto;
    }

    private async Task SyncExpiredStatusAsync(UserVoucher userVoucher)
    {
        if (userVoucher.Status.Equals("Available", StringComparison.OrdinalIgnoreCase) &&
            userVoucher.Voucher.EndDate < DateTime.UtcNow)
        {
            userVoucher.Status = "Expired";
            await _userVoucherRepository.UpdateAsync(userVoucher);
        }
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new Exception("Voucher code is required");
        }

        return code.Trim().ToUpperInvariant();
    }

    private static void ValidateVoucherEligibility(Voucher voucher)
    {
        var now = DateTime.UtcNow;

        if (!voucher.IsActive)
        {
            throw new Exception("This voucher is not active");
        }

        if (now < voucher.StartDate)
        {
            throw new Exception("This voucher is not yet valid");
        }

        if (now > voucher.EndDate)
        {
            throw new Exception("This voucher has expired");
        }

        if (voucher.UsedCount >= voucher.AvailableCount)
        {
            throw new Exception("This voucher has reached its usage limit");
        }
    }

    private async Task ValidateTourForApplyAsync(Voucher voucher, int? tourId)
    {
        if (!voucher.TourId.HasValue)
        {
            return;
        }

        if (!tourId.HasValue)
        {
            throw new Exception("TourId is required for this tour-specific voucher");
        }

        if (voucher.TourId.Value != tourId.Value)
        {
            throw new Exception("This voucher is not applicable to the selected tour");
        }

        var tour = await _tourValidationService.ValidateTourAsync(tourId.Value);
        if (!tour.Exists)
        {
            throw new Exception($"Tour with Id {tourId.Value} not found");
        }

        if (tour.Status != null && !tour.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"Tour with Id {tourId.Value} is not active");
        }
    }
}
