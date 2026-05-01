using Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace UnitTests.Infrastructure.Services;

public class CacheServiceTests
{
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());

    [Fact]
    public async Task GetOrCreateAsync_InvokesFactory_OnFirstCall()
    {
        var sut = new CacheService(_memoryCache);
        var calls = 0;

        var result = await sut.GetOrCreateAsync(
            "k",
            _ => { calls++; return Task.FromResult(123); },
            TimeSpan.FromMinutes(1));

        Assert.Equal(123, result);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task GetOrCreateAsync_DoesNotInvokeFactory_OnCacheHit()
    {
        var sut = new CacheService(_memoryCache);
        var calls = 0;

        await sut.GetOrCreateAsync("k", _ => { calls++; return Task.FromResult(1); }, TimeSpan.FromMinutes(1));
        var second = await sut.GetOrCreateAsync("k", _ => { calls++; return Task.FromResult(2); }, TimeSpan.FromMinutes(1));

        Assert.Equal(1, second);
        Assert.Equal(1, calls);
    }
}
