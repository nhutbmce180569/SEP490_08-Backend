using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Repositories
{
    public interface ITourRepository
    {
        Task Add(Tour model);
        Task<TourPageResult> GetPagedAsync(TourQueryOptions options);
        Task<Tour> GetById(int id);
        void Update(Tour model);
        Task Delete(int id);
        Task SaveChangesAsync();
        Task<int> CountByCategoryIdAsync(int categoryId);
    }
}
