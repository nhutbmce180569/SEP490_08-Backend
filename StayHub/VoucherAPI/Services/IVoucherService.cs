using VoucherAPI.DTOs;

namespace VoucherAPI.Services;

public interface IVoucherService
{
    Task<PaginationDTO<ReadVoucherDTO>> GetAll(
        int page,
        int pageSize,
        string? search,
        int? tourId,
        string? discountType,
        string? status,
        bool? isActive,
        bool? createdByMe,
        int currentUserId);

    Task<ReadVoucherDetailDTO?> GetById(int id);

    Task<ReadVoucherDetailDTO> Create(CreateVoucherDTO dto, int creatorId, bool isAdmin);

    Task<ReadVoucherDetailDTO> Update(int id, UpdateVoucherDTO dto, int currentUserId, bool isAdmin);

    Task<ReadVoucherDTO> Activate(int id, int currentUserId, bool isAdmin);

    Task<ReadVoucherDTO> Deactivate(int id, int currentUserId, bool isAdmin);

    Task<object> DistributeBirthdayVoucherAsync(int month, int currentAdminId);
}
