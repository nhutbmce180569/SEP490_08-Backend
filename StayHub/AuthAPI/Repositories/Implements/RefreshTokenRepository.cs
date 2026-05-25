using AuthAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Repositories.Implements
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly StayHubIdentityDbContext _context;

        public RefreshTokenRepository(StayHubIdentityDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByToken(string token)
        {
            return await _context.RefreshTokens
                .Include(t => t.User) // Load luôn User để check SecurityStamp sau này
                .FirstOrDefaultAsync(t => t.Token == token);
        }

        public async Task Add(RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        public async Task Update(RefreshToken token)
        {
            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAllByUserId(int userId)
        {
            var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();

            if (tokens.Any())
            {
                _context.RefreshTokens.RemoveRange(tokens);
                await _context.SaveChangesAsync();
            }
        }

        // BỔ SUNG HÀM NÀY ĐỂ KHỚP VỚI INTERFACE
        public async Task DeleteExpired()
        {
            // Lấy danh sách các token đã hết hạn hoặc đã bị thu hồi (Revoked) trước đó
            var expiredTokens = await _context.RefreshTokens
                .Where(t => t.Expires < DateTime.Now || t.RevokedAt != null)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _context.RefreshTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();
            }
        }
    }
}