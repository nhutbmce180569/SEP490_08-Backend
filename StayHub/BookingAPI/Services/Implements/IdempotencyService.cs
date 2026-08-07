using BookingAPI.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace BookingAPI.Services.Implements
{
    public class IdempotencyService : IIdempotencyService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly TimeSpan _ttl = TimeSpan.FromHours(24);
        private readonly ILogger<IdempotencyService> _logger;

        public IdempotencyService(IConnectionMultiplexer redis, ILogger<IdempotencyService> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        private IDatabase Db => _redis.GetDatabase();

        private string GetRedisKey(string idempotencyKey, int userId)
        {
            return $"idempotency:create-order:{userId}:{idempotencyKey}";
        }

        public async Task<bool> TryAcquireLockAsync(string idempotencyKey, int userId)
        {
            var key = GetRedisKey(idempotencyKey, userId);
            var state = new IdempotencyState { Status = "Processing" };
            var json = JsonSerializer.Serialize(state);

            _logger.LogInformation("Acquiring idempotency lock for key: {Key}", key);
            var acquired = await Db.StringSetAsync(key, json, _ttl, When.NotExists);

            if (!acquired)
            {
                _logger.LogWarning("Duplicate request detected. Idempotency lock already exists for key: {Key}", key);
            }

            return acquired;
        }

        public async Task<IdempotencyState?> GetStateAsync(string idempotencyKey, int userId)
        {
            var key = GetRedisKey(idempotencyKey, userId);
            var value = await Db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<IdempotencyState>(value.ToString());
        }

        public async Task SetCompletedAsync(string idempotencyKey, int userId, int orderId)
        {
            var key = GetRedisKey(idempotencyKey, userId);
            var state = new IdempotencyState { Status = "Completed", OrderId = orderId };
            var json = JsonSerializer.Serialize(state);

            _logger.LogInformation("Updating idempotency state to Completed for key: {Key}, OrderId: {OrderId}", key, orderId);
            await Db.StringSetAsync(key, json, _ttl);
        }

        public async Task DeleteProcessingKeyAsync(string idempotencyKey, int userId)
        {
            var key = GetRedisKey(idempotencyKey, userId);

            var currentState = await GetStateAsync(idempotencyKey, userId);
            if (currentState != null && currentState.Status == "Processing")
            {
                _logger.LogWarning("Idempotency processing failed. Deleting processing key: {Key}", key);
                await Db.KeyDeleteAsync(key);
            }
            else
            {
                _logger.LogInformation("Key is not Processing or does not exist, skipping deletion. Key: {Key}", key);
            }
        }
    }
}