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
        bool? isActive);

    Task<ReadVoucherDetailDTO?> GetById(int id);

    Task<ReadVoucherDetailDTO> Create(CreateVoucherDTO dto, int creatorId);

    Task<ReadVoucherDetailDTO> Update(int id, UpdateVoucherDTO dto);

    Task<ReadVoucherDTO> Activate(int id);

    Task<ReadVoucherDTO> Deactivate(int id);
}
