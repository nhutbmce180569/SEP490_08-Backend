using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface IPromotionService
    {
        Task<(List<ReadPromotionDTO> Promotions, int Total)> GetAllPromotionsAsync(int page, int pageSize, string? searchTerm = null, string? status = null);
        Task<ReadPromotionDTO> GetPromotionByIdAsync(int id);
        Task<ReadPromotionDTO> CreatePromotionAsync(CreatePromotionDTO dto);
        Task<ReadPromotionDTO> UpdatePromotionAsync(int id, UpdatePromotionDTO dto);
        Task<ReadPromotionDTO> ChangePromotionStatusAsync(int id, string status);
    }
}
