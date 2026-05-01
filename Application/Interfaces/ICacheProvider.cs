using Application.Interfaces.AssemblyMaker;

namespace Application.Interfaces
{
    public interface ICacheProvider : IServiceAssemblyMarker
    {
        Task<TItem> GetOrCreateAsync<TItem>(
            string key,
            Func<CancellationToken, Task<TItem>> factory,
            TimeSpan ttl,
            CancellationToken cancellationToken = default);
    }
}
