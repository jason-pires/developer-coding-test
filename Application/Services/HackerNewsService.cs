using Application.Interfaces;
using Domain.DTO;
using Infrastructure.Interfaces;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class HackerNewsService : IHackerNewsService
    {
        private readonly ILogger<HackerNewsService> _logger;
        private readonly IMapper _mapper;
        private readonly IHackerNewsRestClient _hnRestClient;

        public HackerNewsService(
            ILogger<HackerNewsService> logger,
            IMapper mapper,
            IHackerNewsRestClient hnRestClient
            )
        {
            _logger = logger;
            _mapper = mapper;
            _hnRestClient = hnRestClient;
        }

        public async Task<List<StoryDetailDTO>> GetNSortedStoryDetailsAsync(int n)
        {
            _logger.LogInformation("Fetching top {N} story IDs from Hacker News", n);
            var storiesIDs = await _hnRestClient.GetBestStoriesIdsAsync();
            var topNStoriesIDs = storiesIDs.Take(n).ToList();

            _logger.LogInformation("Fetching details for top {N} stories", n);
            var storyDetailsTasks = topNStoriesIDs.Select(_hnRestClient.GetStoryDetailByIdAsync);
            _logger.LogInformation("Waiting for all story details to be fetched");
            var storyDetails = await Task.WhenAll(storyDetailsTasks);

            _logger.LogInformation("All story details have been fetched");
            return storyDetails.Select(_mapper.Map<StoryDetailDTO>).ToList();
        }
    }
}
