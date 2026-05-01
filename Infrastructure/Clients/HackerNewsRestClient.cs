using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Clients
{
    public class HackerNewsRestClient : IHackerNewsRestClient
    {
        private readonly ILogger<HackerNewsRestClient> _logger;
        private readonly IGenericHttpService _httpService;

        public HackerNewsRestClient(
            ILogger<HackerNewsRestClient> logger,
            IGenericHttpService httpService)
        {
            _logger = logger;
            _httpService = httpService;
        }

        public async Task<BestStoryResponse?> GetStoryDetailByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpService.GetAsync<BestStoryResponse>($"/item/{id}.json", cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HackerNewsRestClient.GetStoryDetailByIdAsync, Id: {StoryId}", id);
                throw;
            }
        }

        public async Task<IReadOnlyList<int>> GetBestStoriesIdsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpService.GetAsync<List<int>>("/beststories.json", cancellationToken: cancellationToken)
                    ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HackerNewsRestClient.GetTopStoriesIdsAsync");
                throw;
            }
        }
    }
}
