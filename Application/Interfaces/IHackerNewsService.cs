using Application.Interfaces.AssemblyMaker;
using Domain.DTO;

namespace Application.Interfaces
{
    public interface IHackerNewsService : IServiceAssemblyMarker
    {
        Task<List<StoryDetailDTO>> GetNSortedStoryDetailsAsync(int n, CancellationToken cancellationToken = default);
    }
}
