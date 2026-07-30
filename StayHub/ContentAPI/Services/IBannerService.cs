using ContentAPI.DTOs;

namespace ContentAPI.Services
{
    public interface IBannerService
    {
        Task<PaginationDTO<ReadBannerDTO>> GetAllBanners(int page, int pageSize);
        Task<PaginationDTO<ReadBannerDTO>> SearchBannersAsync(string keyword, int page, int pageSize);
        Task<PaginationDTO<ReadBannerDTO>> GetActiveBanners(int page, int pageSize);
        Task<ReadBannerDTO?> GetBannerById(int id);
        Task<ReadBannerDTO> CreateBanner(CreateBannerDTO dto);
        Task<bool> UpdateBanner(int id, UpdateBannerDTO dto);
        Task<bool> DeleteBanner(int id);
        Task<bool> ChangeBannerStatus(int id, bool isActive);
    }
}