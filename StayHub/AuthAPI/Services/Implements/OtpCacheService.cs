using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace AuthAPI.Services.Implements
{
    public class OtpCacheService : IOtpCacheService
    {
        private readonly IConnectionMultiplexer _redis;

        public OtpCacheService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        private IDatabase Db => _redis.GetDatabase();

        public async Task<bool> TrySetForgotPasswordCooldownAsync(string email, TimeSpan duration)
        {
            var key = $"forgot_pwd_cooldown_{email}";
            return await Db.StringSetAsync(key, "1", duration, When.NotExists);
        }

        public async Task<TimeSpan?> GetForgotPasswordCooldownRemainingAsync(string email)
        {
            var key = $"forgot_pwd_cooldown_{email}";
            return await Db.KeyTimeToLiveAsync(key);
        }

        public async Task SetForgotPasswordOtpAsync(string email, string otp, TimeSpan duration)
        {
            var key = $"forgot_pwd_otp_{email}";
            await Db.StringSetAsync(key, otp, duration);
        }

        public async Task<string?> GetForgotPasswordOtpAsync(string email)
        {
            var key = $"forgot_pwd_otp_{email}";
            var value = await Db.StringGetAsync(key);
            return value.IsNullOrEmpty ? null : value.ToString();
        }

        public async Task DeleteForgotPasswordOtpAsync(string email)
        {
            var key = $"forgot_pwd_otp_{email}";
            await Db.KeyDeleteAsync(key);
        }

        public async Task DeleteForgotPasswordCooldownAsync(string email)
        {
            var key = $"forgot_pwd_cooldown_{email}";
            await Db.KeyDeleteAsync(key);
        }

        public async Task SetResetPasswordTokenAsync(string email, string resetToken, TimeSpan duration)
        {
            var key = $"reset_pwd_token_{email}";
            await Db.StringSetAsync(key, resetToken, duration);
        }

        public async Task<string?> GetResetPasswordTokenAsync(string email)
        {
            var key = $"reset_pwd_token_{email}";
            var value = await Db.StringGetAsync(key);
            return value.IsNullOrEmpty ? null : value.ToString();
        }

        public async Task DeleteResetPasswordTokenAsync(string email)
        {
            var key = $"reset_pwd_token_{email}";
            await Db.KeyDeleteAsync(key);
        }

        public async Task RevokeUserSessionAsync(int userId, TimeSpan duration)
        {
            var key = $"revoke_user_{userId}";
            long currentUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await Db.StringSetAsync(key, currentUnixTimestamp, duration);
        }

        public async Task<bool> TrySetRegisterCooldownAsync(string email, TimeSpan duration)
        {
            var key = $"reg_cooldown_{email}";
            return await Db.StringSetAsync(key, "1", duration, When.NotExists);
        }

        public async Task<TimeSpan?> GetRegisterCooldownRemainingAsync(string email)
        {
            var key = $"reg_cooldown_{email}";
            return await Db.KeyTimeToLiveAsync(key);
        }

        public async Task SetRegisterOtpAsync(string email, string otp, TimeSpan duration)
        {
            var key = $"reg_otp_{email}";
            await Db.StringSetAsync(key, otp, duration);
        }

        public async Task<string?> GetRegisterOtpAsync(string email)
        {
            var key = $"reg_otp_{email}";
            var value = await Db.StringGetAsync(key);
            return value.IsNullOrEmpty ? null : value.ToString();
        }

        public async Task DeleteRegisterOtpAsync(string email)
        {
            var key = $"reg_otp_{email}";
            await Db.KeyDeleteAsync(key);
        }

        public async Task DeleteRegisterCooldownAsync(string email)
        {
            var key = $"reg_cooldown_{email}";
            await Db.KeyDeleteAsync(key);
        }

        public async Task<int> IncrementFailedLoginAsync(string email)
        {
            var key = $"failed_login_{email}";
            long count = await Db.StringIncrementAsync(key);
            // Set an expiration of 24 hours so failed attempts eventually clear out
            await Db.KeyExpireAsync(key, TimeSpan.FromHours(24));
            return (int)count;
        }

        public async Task ResetFailedLoginAsync(string email)
        {
            var key = $"failed_login_{email}";
            await Db.KeyDeleteAsync(key);
        }

        public async Task SetLockoutAsync(string email, TimeSpan duration)
        {
            var key = $"lockout_{email}";
            var expireTime = DateTime.UtcNow.Add(duration);
            await Db.StringSetAsync(key, expireTime.ToString("o"), duration);
        }

        public async Task<TimeSpan?> GetLockoutRemainingAsync(string email)
        {
            var key = $"lockout_{email}";
            var value = await Db.StringGetAsync(key);
            if (value.HasValue && DateTime.TryParse(value.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var expireTime))
            {
                var remaining = expireTime.ToUniversalTime() - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : null;
            }
            return null;
        }
    }
}
