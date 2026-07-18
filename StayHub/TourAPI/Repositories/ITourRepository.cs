using TourAPI.Models;

namespace TourAPI.Repositories
{
    public interface ITourRepository
    {
        Task Add(Tour model);
        Task<(List<Tour> Tours, int Total)> GetAll(
            int page,
            int pageSize,
            string? searchTerm = null,
            int? categoryId = null,
            int? createdBy = null);
        Task<(List<Tour> Tours, int Total)> GetActiveTours(int page, int pageSize);
        Task<(List<Tour> Tours, int Total)> SearchTours(
            int page,
            int pageSize,
            string? searchTerm = null,
            int? categoryId = null,
            string? country = null,
            string? city = null,
            long? minPrice = null,
            long? maxPrice = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? duration = null,
            string? sortBy = null);
        Task<List<Tour>> GetToursByRegion(string region, int limit = 6);
        Task<(List<Tour> Tours, int Total)> GetByAdmin(
            int page,
            int pageSize,
            string? searchTerm = null,
            int? managerId = null);
        Task<(List<Tour> Tours, int Total)> GetByManager(
            int managerId,
            int page,
            int pageSize,
            string? searchTerm = null);
        Task<List<Tour>> GetSaleTours(int limit = 6);
        Task<List<Tour>> GetHotTours(int limit = 5);
        Task<List<Tour>> GetUpcomingTours(int limit = 6);
        Task<Tour> GetById(int id);
        void Update(Tour model);
        Task Delete(int id);
        Task SaveChangesAsync();
        Task<int> CountByCategoryIdAsync(int categoryId);
    }
}
