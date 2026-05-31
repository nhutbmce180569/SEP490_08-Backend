using ContentAPI.Models;

namespace ContentAPI.Repositories
{
    public interface ITourismInformationRepository
    {
        Task<(List<TourismInformation> Items, int Total)> GetAllPagedAsync(
            int page,
            int pageSize,
            string? searchTerm,
            string? type,
            string? status,
            string? city);

        Task<(List<TourismInformation> Items, int Total)> GetActivePagedAsync(int page, int pageSize);

        Task<TourismInformation?> GetByIdAsync(int id);

        Task AddAsync(TourismInformation model);

        Task UpdateAsync(TourismInformation model);
    }
}
