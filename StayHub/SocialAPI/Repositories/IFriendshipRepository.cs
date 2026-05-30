using System.Collections.Generic;
using System.Threading.Tasks;
using SocialAPI.Models;

namespace SocialAPI.Repositories;

public interface IFriendshipRepository
{
    Task<IEnumerable<Friendship>> GetFriendshipsByUserIdAsync(int userId);
    Task<IEnumerable<Friendship>> GetPendingRequestsAsync(int userId);
    Task<Friendship> AddAsync(Friendship friendship);
    Task UpdateAsync(Friendship friendship);
    Task DeleteAsync(int id);
    Task<bool> CheckExistingFriendship(int user1, int user2);
    Task<Friendship?> GetByIdAsync(int id);
    Task<(List<Friendship> Friends, int Total)> GetFriendListPagedAsync(int userId, int page, int pageSize);
    Task<bool> CheckAreFriendsAsync(int userId1, int userId2);
}