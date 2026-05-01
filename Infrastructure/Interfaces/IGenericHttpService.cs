using Infrastructure.Interfaces.AssemblyMaker;

namespace Infrastructure.Interfaces
{
    public interface IGenericHttpService : IInfrastructureAssemblyMarker
    {
        Task<TResponse?> GetAsync<TResponse>(string uri, List<KeyValuePair<string, string>>? query = null, CancellationToken cancellationToken = default)
            where TResponse : class;
    }
}
