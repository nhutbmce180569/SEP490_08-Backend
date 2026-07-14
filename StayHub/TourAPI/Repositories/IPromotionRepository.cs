using TourAPI.Models;

namespace TourAPI.Repositories
{
    public interface IPromotionRepository
    {
        Task<(List<Promotion> Promotions, int Total)> GetAll(
            int page,
            int pageSize,
            string? searchTerm = null,
            string? status = null);
        Task<Promotion?> GetById(int id);
        Task<Promotion?> GetByCode(string code);
        Task Add(Promotion model);
        void Update(Promotion model);
        void Delete(Promotion model);
        Task SaveChangesAsync();
    }
}
