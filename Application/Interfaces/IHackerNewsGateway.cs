using Application.Interfaces.AssemblyMaker;
using Domain.Responses;

namespace Application.Interfaces
{
    public interface IHackerNewsGateway : IServiceAssemblyMarker
    {
        Task<IReadOnlyList<int>> GetBestStoriesIdsAsync(CancellationToken cancellationToken = default);
        Task<BestStoryResponse?> GetStoryDetailByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}
