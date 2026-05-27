using ContentAPI.Models;

namespace ContentAPI.Repositories
{
    public interface ICategoryRepository
    {
        Task<(List<Category> Categories, int Total)> GetAllPaged(int page, int pageSize);
        Task<(List<Category> Categories, int Total)> SearchPagedAsync(string keyword, int page, int pageSize);
        Task<(List<Category> Categories, int Total)> GetActiveCategoriesPaged(int page, int pageSize);
        Task<Category?> GetById(int id);
        Task<Category?> GetBySlug(string slug);
        Task Add(Category model);
        Task Update(int id, Category model);
        Task Delete(int id);
    }
}