using ContentAPI.Models;

namespace ContentAPI.Repositories
{
    public interface IBannerRepository
    {
        Task<(List<Banner> Banners, int Total)> GetAllPaged(int page, int pageSize);
        Task<(List<Banner> Banners, int Total)> GetActiveBannersPaged(int page, int pageSize);
        Task<Banner?> GetById(int id);
        Task<Banner> Add(Banner banner);
        Task Update(int id, Banner banner);
        Task Delete(int id);
    }
}