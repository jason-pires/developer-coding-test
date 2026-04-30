using Domain.Responses;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Clients
{
    public class HackerNewsRestClient : IHackerNewsRestClient
    {
        private readonly ILogger<HackerNewsRestClient> _logger;
        private readonly IGenericRestClient _restClient;

        public HackerNewsRestClient(
            ILogger<HackerNewsRestClient> logger,
            IGenericRestClient restClient
            )
        {
            _logger = logger;
            _restClient = restClient;
        }

        public async Task<BestStoryResponse> GetStoryDetailByIdAsync(int id)
        {
            try
            {
                return await _restClient.GetAsync<BestStoryResponse>($"/item/{id}.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in HackerNewsRestClient.GetStoryDetailByIdAsync, Id: {id}");
                throw;
            }
        }

        public async Task<List<int>> GetBestStoriesIdsAsync()
        {
            try
            {
                return await _restClient.GetAsync<List<int>>("/beststories.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HackerNewsRestClient.GetTopStoriesIdsAsync");
                throw;
            }
        }
    }
}
