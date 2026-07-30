using System;
using System.Threading.Tasks;

namespace AuthAPI.Services
{
    public interface IOtpCacheService
    {
        Task<bool> TrySetForgotPasswordCooldownAsync(string email, TimeSpan duration);
        Task<TimeSpan?> GetForgotPasswordCooldownRemainingAsync(string email);
        Task SetForgotPasswordOtpAsync(string email, string otp, TimeSpan duration);
        Task<string?> GetForgotPasswordOtpAsync(string email);
        Task DeleteForgotPasswordOtpAsync(string email);
        Task DeleteForgotPasswordCooldownAsync(string email);
        Task SetResetPasswordTokenAsync(string email, string resetToken, TimeSpan duration);
        Task<string?> GetResetPasswordTokenAsync(string email);
        Task DeleteResetPasswordTokenAsync(string email);
        Task RevokeUserSessionAsync(int userId, TimeSpan duration);

        Task<bool> TrySetRegisterCooldownAsync(string email, TimeSpan duration);
        Task<TimeSpan?> GetRegisterCooldownRemainingAsync(string email);
        Task SetRegisterOtpAsync(string email, string otp, TimeSpan duration);
        Task<string?> GetRegisterOtpAsync(string email);
        Task DeleteRegisterOtpAsync(string email);
        Task DeleteRegisterCooldownAsync(string email);
    }
}
