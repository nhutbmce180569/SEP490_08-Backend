using AuthAPI.DTOs;
using AuthAPI.Models;

namespace AuthAPI.Repositories
{
    public interface IUserRepository
    {
        Task Add(User model);
        Task<List<User>> GetAll();
        Task<(List<User> Users, int Total)> GetAllPaged(int page, int pageSize);

        Task<string?> GetFcmTokenAsync(int userId);
        Task<User?> GetById(int id);
        Task<User?> GetByEmail(string email);
        Task<User?> GetByPhoneNumber(string phoneNumber);
        Task Update(int id, User model);
        Task Delete(int id);

        Task<(List<User> Users, int Total)> SearchPagedAsync(string query, int page, int pageSize, string? roleName = null);
        Task<List<User>> GetUsersByIdsAsync(List<int> ids);
        Task<(List<User> Users, int Total)> FilterPagedAsync(UserFilterDTO filter);

        Task<List<User>> GetAllCustomersAsync();

        Task<(List<User> Users, int Total)> GetCustomersPagedAsync(string? search, int page, int pageSize);
    }
}