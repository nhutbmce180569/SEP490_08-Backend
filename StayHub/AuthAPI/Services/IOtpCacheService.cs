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
        Task RevokeUserSessionAsync(int userId, TimeSpan duration);
    }
}
