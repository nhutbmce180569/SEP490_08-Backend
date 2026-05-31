using ContentAPI.DTOs;

namespace ContentAPI.Services
{
    public interface ITourismInformationService
    {
        Task<PaginationDTO<ReadTourismInformationDTO>> GetAllAsync(
            int page,
            int pageSize,
            string? searchTerm,
            string? type,
            string? status,
            string? city);

        Task<PaginationDTO<ReadTourismInformationDTO>> GetActiveAsync(int page, int pageSize);

        Task<ReadTourismInformationDTO?> GetByIdAsync(int id);

        Task<ReadTourismInformationDTO> CreateAsync(CreateTourismInformationDTO dto);

        Task<bool> UpdateAsync(int id, UpdateTourismInformationDTO dto);

        Task<bool> ChangeStatusAsync(int id, bool isActive);
    }
}
