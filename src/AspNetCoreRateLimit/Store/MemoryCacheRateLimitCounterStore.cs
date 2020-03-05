using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public class MemoryCacheRateLimitCounterStore : MemoryCacheRateLimitStore<RateLimitCounter?>, IRateLimitCounterStore
    {
        /// The key-lock used for limiting requests.
        private static readonly AsyncKeyLock AsyncLock = new AsyncKeyLock();
        private readonly IMemoryCache _cache;

        public MemoryCacheRateLimitCounterStore(IMemoryCache cache) : base(cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<RateLimitCounter> IncrementAsync(string counterId, TimeSpan interval, Func<double> RateIncrementer = null)
        {
            using (await AsyncLock.WriterLockAsync(counterId).ConfigureAwait(false))
            {
                // Determine current interval and its start
                var now = DateTime.UtcNow;
                var intervalNumber = now.Ticks / interval.Ticks;
                var intervalStart = new DateTime(intervalNumber * interval.Ticks, DateTimeKind.Utc);
                var count = await _cache.GetOrCreateAsync(counterId, entry => {
                    // Set the cache entry expiry to the end of the current interval
                    var intervalEnd = intervalStart + interval;
                    entry.SetAbsoluteExpiration(intervalEnd - now);
                    return Task.FromResult(0D);
                });

                count += RateIncrementer?.Invoke() ?? 1D;
                _cache.Set(counterId, count);
                return new RateLimitCounter
                {
                    Count = count,
                    Timestamp = intervalStart
                };
            }
        }
    }
}