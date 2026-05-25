using AuthAPI.DTOs;

namespace AuthAPI.Services
{
    public interface IRoleService
    {
        Task<List<ReadRoleDTO>> GetAllRoles();
        Task<ReadRoleDTO?> GetRoleById(int id);
        Task<ReadRoleDTO?> GetRoleByName(string name);
    }
}