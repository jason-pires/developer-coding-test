using Infrastructure.Interfaces.AssemblyMaker;

namespace Infrastructure.Interfaces
{
    public interface IGenericRestClient : IInfrastructureAssemblyMarker
    {
        Task<TResponse> GetAsync<TResponse>(string uri, List<KeyValuePair<string, string>> query = null)
            where TResponse : class;
    }
}
