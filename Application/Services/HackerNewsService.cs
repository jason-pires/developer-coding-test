using Application.Interfaces;
using Common.Config;
using Domain.DTO;
using Domain.Responses;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services
{
    public class HackerNewsService : IHackerNewsService
    {
        private const string BestStoriesCacheKey = "hackernews:beststories";

        private readonly ILogger<HackerNewsService> _logger;
        private readonly IMapper _mapper;
        private readonly IHackerNewsGateway _hackerNewsGateway;
        private readonly ICacheProvider _cacheProvider;
        private readonly HackerNewsOptions _options;

        public HackerNewsService(
            ILogger<HackerNewsService> logger,
            IMapper mapper,
            IHackerNewsGateway hackerNewsGateway,
            ICacheProvider cacheProvider,
            IOptions<HackerNewsOptions> options)
        {
            _logger = logger;
            _mapper = mapper;
            _hackerNewsGateway = hackerNewsGateway;
            _cacheProvider = cacheProvider;
            _options = options.Value;
        }

        public async Task<List<StoryDetailDTO>> GetNSortedStoryDetailsAsync(int n, CancellationToken cancellationToken = default)
        {
            var boundedCount = Math.Min(n, _options.MaxStoriesPerRequest);

            _logger.LogInformation("Fetching top {N} story IDs from Hacker News", boundedCount);

            var storyIds = await _cacheProvider.GetOrCreateAsync(
                BestStoriesCacheKey,
                ct => _hackerNewsGateway.GetBestStoriesIdsAsync(ct),
                TimeSpan.FromSeconds(_options.BestStoriesCacheSeconds),
                cancellationToken);

            var selectedStoryIds = storyIds.Take(boundedCount).ToList();

            _logger.LogInformation(
                "Fetching details for top {N} stories with concurrency limit {ConcurrencyLimit}",
                boundedCount,
                _options.MaxConcurrentStoryRequests);

            var storyDetails = new BestStoryResponse?[selectedStoryIds.Count];
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = _options.MaxConcurrentStoryRequests
            };

            await Parallel.ForEachAsync(
                selectedStoryIds.Select((storyId, index) => (storyId, index)),
                parallelOptions,
                async (item, ct) =>
                {
                    storyDetails[item.index] = await _cacheProvider.GetOrCreateAsync(
                        $"hackernews:story:{item.storyId}",
                        cacheToken => _hackerNewsGateway.GetStoryDetailByIdAsync(item.storyId, cacheToken),
                        TimeSpan.FromSeconds(_options.StoryDetailsCacheSeconds),
                        ct);
                });

            _logger.LogInformation("All story details have been fetched");

            return storyDetails
                .Where(story => story is not null)
                .Select(story => _mapper.Map<StoryDetailDTO>(story!))
                .ToList();
        }
    }
}
