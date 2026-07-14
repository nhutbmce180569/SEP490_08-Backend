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

        public async Task RevokeUserSessionAsync(int userId, TimeSpan duration)
        {
            var key = $"revoke_user_{userId}";
            long currentUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await Db.StringSetAsync(key, currentUnixTimestamp, duration);
        }
    }
}
