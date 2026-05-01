using Infrastructure.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Services
{
    public class CacheService(IMemoryCache memoryCache) : ICacheService
    {
        private readonly IMemoryCache _memoryCache = memoryCache;

        public Task<TItem> GetOrCreateAsync<TItem>(
            string key,
            Func<CancellationToken, Task<TItem>> factory,
            TimeSpan ttl,
            CancellationToken cancellationToken = default)
        {
            return _memoryCache.GetOrCreateAsync(
                key,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = ttl;
                    return await factory(cancellationToken);
                })!;
        }
    }
}
