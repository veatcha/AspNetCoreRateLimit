using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System;
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

            // Increment the counter and expire the key if required
            var count = await _redis.GetDatabase().StringIncrementAsync(counterId, 1);
            if (count == 1)
            {
                var intervalEnd = (intervalStart + interval) - now;
                await _redis.GetDatabase().KeyExpireAsync(counterId, intervalEnd);
            }

            return new RateLimitCounter
            {
                Count = count,
                Timestamp = intervalStart
            };
        }
    }
}