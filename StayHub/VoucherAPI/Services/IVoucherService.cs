using VoucherAPI.DTOs;

namespace VoucherAPI.Services;

public interface IVoucherService
{
    Task<PaginationDTO<ReadVoucherDTO>> GetAll(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        int? tourId = null,
        string? discountType = null,
        string? status = null,
        bool? isActive = null,
        bool? createdByMe = null,
        int currentUserId = 0,
        string? voucherType = null);

    Task<ReadVoucherDetailDTO?> GetById(int id);

    Task<ReadVoucherDetailDTO> Create(CreateVoucherDTO dto, int creatorId, bool isAdmin);

    Task<ReadVoucherDetailDTO> Update(int id, UpdateVoucherDTO dto, int currentUserId, bool isAdmin);

    Task<ReadVoucherDTO> Activate(int id, int currentUserId, bool isAdmin);

    Task<ReadVoucherDTO> Deactivate(int id, int currentUserId, bool isAdmin);

    Task<object> DistributeBirthdayVoucherAsync(int month, int currentAdminId);
    Task<bool> CheckBirthdayVoucherDistributedAsync(int month, int year);
}
