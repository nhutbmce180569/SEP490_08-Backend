using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SocialAPI.Models;

namespace SocialAPI.Repositories.Implements;

public class FriendshipRepository : IFriendshipRepository
{
    private readonly StayHubSocialDbContext _context;

    public FriendshipRepository(StayHubSocialDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Friendship>> GetFriendshipsByUserIdAsync(int userId)
    {
        return await _context.Friendships
            .Where(f => (f.RequesterId == userId || f.ReceiverId == userId) && f.Status == "Accepted")
            .ToListAsync();
    }

    public async Task<IEnumerable<Friendship>> GetPendingRequestsAsync(int userId)
    {
        return await _context.Friendships
            .Where(f => f.ReceiverId == userId && f.Status == "Pending")
            .ToListAsync();
    }

    public async Task<Friendship> AddAsync(Friendship friendship)
    {
        _context.Friendships.Add(friendship);
        await _context.SaveChangesAsync();
        return friendship;
    }

    public async Task UpdateAsync(Friendship friendship)
    {
        _context.Friendships.Update(friendship);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var friendship = await _context.Friendships.FindAsync(id);
        if (friendship != null)
        {
            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> CheckExistingFriendship(int user1, int user2)
    {
        return await _context.Friendships.AnyAsync(f =>
            (f.RequesterId == user1 && f.ReceiverId == user2) ||
            (f.RequesterId == user2 && f.ReceiverId == user1));
    }

    public async Task<Friendship?> GetByIdAsync(int id)
    {
        return await _context.Friendships.FindAsync(id);
    }

    public async Task<(List<Friendship> Friends, int Total)> GetFriendListPagedAsync(int userId, int page, int pageSize)
    {
        var query = _context.Friendships
            .Where(f => f.Status == "Accepted" && (f.RequesterId == userId || f.ReceiverId == userId))
            .AsNoTracking();

        int total = await query.CountAsync();

        var friends = await query
            .OrderByDescending(f => f.CreatedAt) 
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (friends, total);
    }

    public async Task<bool> CheckAreFriendsAsync(int userId1, int userId2)
    {
        return await _context.Friendships.AnyAsync(f =>
            f.Status == "Accepted" &&
            ((f.RequesterId == userId1 && f.ReceiverId == userId2) ||
             (f.RequesterId == userId2 && f.ReceiverId == userId1)));
    }
}