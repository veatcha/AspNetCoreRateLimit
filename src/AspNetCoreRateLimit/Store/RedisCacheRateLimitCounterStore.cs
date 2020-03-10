using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public class RedisCacheRateLimitCounterStore : IRateLimitCounterStore
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisCacheRateLimitCounterStore(IConnectionMultiplexer redis)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        }

        public async Task<RateLimitCounter> IncrementAsync(string counterId, TimeSpan interval, Func<double> RateIncrementer = null)
        {
            var now = DateTime.UtcNow;
            var intervalNumber = now.Ticks / interval.Ticks;
            var intervalStart = new DateTime(intervalNumber * interval.Ticks, DateTimeKind.Utc);
            counterId = String.Concat(counterId, intervalStart);  // Ensure late clients don't expire the wrong key

            // Increment the counter and expire the key
            var intervalEnd = (intervalStart + interval) - now;
            var transaction = _redis.GetDatabase().CreateTransaction();
            var incr = transaction.StringIncrementAsync(counterId, 1);
            var expireat = transaction.KeyExpireAsync(counterId, intervalEnd);
            if (await transaction.ExecuteAsync() && await expireat)
            {
                return new RateLimitCounter
                {
                    Count = await incr,
                    Timestamp = intervalStart
                };
            }
            throw new ExternalException($"Failed to increment rate limit for key {counterId}");
        }
    }
}