using ContentAPI.DTOs;

namespace ContentAPI.Services
{
    public interface ICategoryService
    {
        Task<PaginationDTO<ReadCategoryDTO>> GetAllCategories(int page, int pageSize);
        Task<PaginationDTO<ReadCategoryDTO>> GetActiveCategories(int page, int pageSize);
        Task<ReadCategoryDTO?> GetCategoryById(int id);
        Task<ReadCategoryDTO> CreateCategory(CreateCategoryDTO dto);
        Task<bool> UpdateCategory(int id, UpdateCategoryDTO dto);
        Task<bool> DeleteCategory(int id);
        Task<bool> ChangeCategoryStatus(int id, bool isActive);
    }
}