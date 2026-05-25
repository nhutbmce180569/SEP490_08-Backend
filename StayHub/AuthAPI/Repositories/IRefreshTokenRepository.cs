using AuthAPI.Models;

namespace AuthAPI.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByToken(string token);
        Task Add(RefreshToken token);
        Task Update(RefreshToken token);
        Task DeleteAllByUserId(int userId);
        Task DeleteExpired();
    }
}
