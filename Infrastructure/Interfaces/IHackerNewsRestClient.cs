using Domain.Responses;
using Infrastructure.Interfaces.AssemblyMaker;

namespace Infrastructure.Interfaces
{
    public interface IHackerNewsRestClient : IInfrastructureAssemblyMarker
    {
        Task<List<int>> GetBestStoriesIdsAsync();
        Task<BestStoryResponse> GetStoryDetailByIdAsync(int id);
    }
}
