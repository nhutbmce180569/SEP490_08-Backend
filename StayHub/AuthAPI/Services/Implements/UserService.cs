using AutoMapper;
using AuthAPI.DTOs;
using AuthAPI.Models;
using AuthAPI.Repositories;
using AuthAPI.Helpers;
using StackExchange.Redis;

namespace AuthAPI.Services.Implements
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHelper _passwordHelper;
        private readonly IMapper _mapper;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IConnectionMultiplexer _redis;

        public UserService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHelper passwordHelper,
            IMapper mapper,
            ICloudinaryService cloudinaryService,
            IConnectionMultiplexer redis)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHelper = passwordHelper;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
            _redis = redis;
        }

        public async Task<PaginationDTO<ReadUserDTO>> GetAllUsers(int page, int pageSize)
        {
            var (users, total) = await _userRepository.GetAllPaged(page, pageSize);
            var userDtos = _mapper.Map<List<ReadUserDTO>>(users);

            return new PaginationDTO<ReadUserDTO>
            {
                Data = userDtos,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<ReadUserDTO?> GetUserById(int id)
        {
            var user = await _userRepository.GetById(id);
            return user == null ? null : _mapper.Map<ReadUserDTO>(user);
        }

        public async Task<ReadUserDTO?> GetUserByEmail(string email)
        {
            var user = await _userRepository.GetByEmail(email);
            return user == null ? null : _mapper.Map<ReadUserDTO>(user);
        }

        public async Task<ReadUserDTO> CreateUserByAdmin(CreateUserDTO createUserDto)
        {
            var existingUser = await _userRepository.GetByEmail(createUserDto.Email);
            if (existingUser != null)
                throw new InvalidOperationException("Email is already in use.");

            var newUser = _mapper.Map<User>(createUserDto);

            if (createUserDto.AvatarFile != null && createUserDto.AvatarFile.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(createUserDto.AvatarFile, "StayHub_Avatars");

                if (uploadResult != null && uploadResult.SecureUrl != null)
                {
                    newUser.AvatarUrl = uploadResult.SecureUrl.ToString();
                }
            }

            newUser.PasswordHash = _passwordHelper.Hash(newUser, createUserDto.Password);
            newUser.Provider = "Local";
            newUser.SecurityStamp = Guid.NewGuid().ToString();

            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;

            newUser.LocPrivacy = true;
            newUser.MomentPrivacy = true;

            newUser.Roles ??= new List<Models.Role>();

            if (createUserDto.RoleIds != null && createUserDto.RoleIds.Any())
            {
                foreach (var roleId in createUserDto.RoleIds)
                {
                    var role = await _roleRepository.GetById(roleId);
                    if (role != null) newUser.Roles.Add(role);
                }
            }
            else
            {
                var defaultRole = await _roleRepository.GetByName("Customer");
                if (defaultRole != null) newUser.Roles.Add(defaultRole);
            }

            await _userRepository.Add(newUser);
            return _mapper.Map<ReadUserDTO>(newUser);
        }

        public async Task<bool> UpdateUserProfile(int id, UpdateUserDTO updateUserDto)
        {
            var existingUser = await _userRepository.GetById(id);
            if (existingUser == null) return false;

            if (updateUserDto.Status == "Blocked" && existingUser.Roles.Any(r => r.Name == "Admin"))
            {
                throw new InvalidOperationException("Cannot block an Admin account.");
            }

            _mapper.Map(updateUserDto, existingUser);

            if (updateUserDto.AvatarFile != null && updateUserDto.AvatarFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(existingUser.AvatarUrl))
                {
                    var publicId = _cloudinaryService.ExtractPublicIdFromUrl(existingUser.AvatarUrl);
                    if (publicId != null)
                    {
                        await _cloudinaryService.DeleteImageAsync(publicId);
                    }
                }

                var uploadResult = await _cloudinaryService.UploadImageAsync(updateUserDto.AvatarFile, "StayHub_Avatars");

                if (uploadResult != null && uploadResult.SecureUrl != null)
                {
                    existingUser.AvatarUrl = uploadResult.SecureUrl.ToString();
                }
            }

            existingUser.SecurityStamp = Guid.NewGuid().ToString();
            existingUser.UpdatedAt = DateTime.UtcNow;

            existingUser.Roles ??= new List<Models.Role>();

            if (updateUserDto.RoleIds != null)
            {
                existingUser.Roles.Clear();
                foreach (var roleId in updateUserDto.RoleIds)
                {
                    var role = await _roleRepository.GetById(roleId);
                    if (role != null) existingUser.Roles.Add(role);
                }
            }


            if (updateUserDto.Status == "Blocked")
            {
                var db = _redis.GetDatabase();
                long currentUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string redisKey = $"revoke_user_{id}";

                await db.StringSetAsync(redisKey, currentUnixTimestamp, TimeSpan.FromMinutes(60));
            }

            await _userRepository.Update(id, existingUser);

            await _refreshTokenRepository.DeleteAllByUserId(id);

            return true;
        }

        public async Task<bool> DeleteUser(int id)
        {
            var existingUser = await _userRepository.GetById(id);
            if (existingUser == null) return false;

            await _refreshTokenRepository.DeleteAllByUserId(id);
            await _userRepository.Delete(id);

            return true;
        }

        public async Task<PaginationDTO<UserSearchResultDto>> SearchUsersAsync(string query, int page, int pageSize, string? role = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new PaginationDTO<UserSearchResultDto>
                {
                    Data = new List<UserSearchResultDto>(),
                    Total = 0,
                    TotalPages = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }

            var (users, total) = await _userRepository.SearchPagedAsync(query, page, pageSize, role);
            var userDtos = _mapper.Map<List<UserSearchResultDto>>(users);

            return new PaginationDTO<UserSearchResultDto>
            {
                Data = userDtos,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<UserProfileDto> GetPublicProfileAsync(int id)
        {
            var user = await _userRepository.GetById(id);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {id} was not found.");
            }

            return _mapper.Map<UserProfileDto>(user);
        }

        public async Task UpdatePrivacyAsync(int userId, PrivacySettingsDto dto)
        {
            var user = await _userRepository.GetById(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            user.LocPrivacy = dto.LocPrivacy;
            user.MomentPrivacy = dto.MomentPrivacy;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.Update(userId, user);
        }

        public async Task<IEnumerable<UserSearchResultDto>> GetUsersBatchAsync(List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                return Enumerable.Empty<UserSearchResultDto>();
            }

            var uniqueIds = userIds.Distinct().ToList();
            var users = await _userRepository.GetUsersByIdsAsync(uniqueIds);


            return _mapper.Map<IEnumerable<UserSearchResultDto>>(users);
        }

        public async Task<bool> ChangeUserStatusAsync(int id, string newStatus)
        {
            var user = await _userRepository.GetById(id);
            if (user == null) return false;

            if (user.Roles.Any(r => r.Name == "Admin"))
            {
                throw new InvalidOperationException("Cannot block or change the status of an Admin account.");
            }

            user.Status = newStatus;
            user.UpdatedAt = DateTime.UtcNow;

            if (newStatus == "Blocked")
            {
                user.SecurityStamp = Guid.NewGuid().ToString();

                await _refreshTokenRepository.DeleteAllByUserId(id);

                var db = _redis.GetDatabase();
                long currentUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string redisKey = $"revoke_user_{id}";

                await db.StringSetAsync(redisKey, currentUnixTimestamp, TimeSpan.FromMinutes(60));
            }

            await _userRepository.Update(id, user);
            return true;

        }

        public async Task<PaginationDTO<ReadUserDTO>> FilterUsersAsync(UserFilterDTO filter)
        {
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 10;

            var (users, total) = await _userRepository.FilterPagedAsync(filter);

            var userDtos = _mapper.Map<List<ReadUserDTO>>(users);

            return new PaginationDTO<ReadUserDTO>
            {
                Data = userDtos,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize),
                CurrentPage = filter.Page,
                PageSize = filter.PageSize
            };
        }
    }
}