using System;
using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public interface IRateLimitCounterStore
    {
        Task<RateLimitCounter> IncrementAsync(string counterId, TimeSpan interval, Func<double> RateIncrementer = null);
    }
}