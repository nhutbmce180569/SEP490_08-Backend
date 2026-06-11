using AuthAPI.DTOs;

namespace AuthAPI.Services
{
    public interface IUserService
    {
        Task<PaginationDTO<ReadUserDTO>> GetAllUsers(int page, int pageSize);

        Task<ReadUserDTO?> GetUserById(int id);
        Task<ReadUserDTO?> GetUserByEmail(string email);
        Task<AdminCreatedUserDTO> CreateUserByAdmin(CreateUserDTO createUserDto);
        Task<bool> UpdateUserProfile(int id, UpdateUserDTO updateUserDto);
        Task<bool> DeleteUser(int id);
        Task<PaginationDTO<UserSearchResultDto>> SearchUsersAsync(string query, int page, int pageSize, string? role = null);
        Task<UserProfileDto> GetPublicProfileAsync(int id);
        Task UpdatePrivacyAsync(int userId, PrivacySettingsDto dto);
        Task<IEnumerable<UserSearchResultDto>> GetUsersBatchAsync(List<int> userIds);
        Task<bool> ChangeUserStatusAsync(int id, string newStatus);
        Task<PaginationDTO<ReadUserDTO>> FilterUsersAsync(UserFilterDTO filter);
        Task<UserProfileResponseDto?> GetUserProfileAsync(int userId);

        Task<CustomerDemographicsDTO> GetCustomerDemographicsAsync(DateTime? from, DateTime? to, string granularity);

        Task<CustomerListAnalyticsDTO> GetCustomersForAnalyticsAsync(string? search, int page, int pageSize);

        Task<PlatformUserStatsDTO> GetPlatformUserStatsAsync(DateTime? from, DateTime? to, string granularity);

        Task<string?> GetFcmTokenAsync(int userId);
        Task<bool> UpdateFcmTokenAsync(int userId, string fcmToken);
    }
}
