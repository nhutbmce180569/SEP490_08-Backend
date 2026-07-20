namespace VoucherAPI.Services;

public interface IUserValidationService
{
    Task<(bool Exists, string? FullName, string? Email, string? Status)> ValidateUserAsync(int userId);
    Task<List<VoucherAPI.DTOs.ReadUserApiDTO>> GetCustomersByBirthdayMonthAsync(int month);
    Task<List<VoucherAPI.DTOs.ReadUserApiDTO>> GetUsersBatchAsync(List<int> userIds);
}
