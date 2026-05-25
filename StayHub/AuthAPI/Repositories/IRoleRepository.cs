using AuthAPI.Models;

namespace AuthAPI.Repositories
{
    public interface IRoleRepository
    {
        Task<Role?> GetByName(string name);
        Task<Role?> GetById(int id);
        Task<List<Role>> GetAll();
    }
}
