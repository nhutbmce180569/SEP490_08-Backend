using AutoMapper;
using AuthAPI.DTOs;
using AuthAPI.Repositories;

namespace AuthAPI.Services.Implements
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;

        public RoleService(IRoleRepository roleRepository, IMapper mapper)
        {
            _roleRepository = roleRepository;
            _mapper = mapper;
        }

        public async Task<List<ReadRoleDTO>> GetAllRoles()
        {
            // Cần đảm bảo IRoleRepository có hàm GetAll()
            var roles = await _roleRepository.GetAll();
            return _mapper.Map<List<ReadRoleDTO>>(roles);
        }

        public async Task<ReadRoleDTO?> GetRoleById(int id)
        {
            var role = await _roleRepository.GetById(id);
            return role == null ? null : _mapper.Map<ReadRoleDTO>(role);
        }

        public async Task<ReadRoleDTO?> GetRoleByName(string name)
        {
            var role = await _roleRepository.GetByName(name);
            return role == null ? null : _mapper.Map<ReadRoleDTO>(role);
        }
    }
}