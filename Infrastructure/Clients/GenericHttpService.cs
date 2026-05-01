using Common.Config;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Registry;
using System.Net;
using System.Text.Json;

namespace Infrastructure.Clients
{
    public class GenericHttpService : IGenericHttpService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly ILogger<GenericHttpService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ResiliencePipelineProvider<string> _pipelineProvider;

        public GenericHttpService(
            IOptions<GenericHttpClientOptions> options,
            ILogger<GenericHttpService> logger,
            IHttpClientFactory httpClientFactory,
            ResiliencePipelineProvider<string> pipelineProvider)
        {
            _httpClient = httpClientFactory.CreateClient(GenericHttpClientOptions.ClientName);
            _httpClient.BaseAddress ??= new Uri(options.Value.BaseUrl);
            _logger = logger;
            _pipelineProvider = pipelineProvider;
        }

        public async Task<TResponse?> GetAsync<TResponse>(string uri, List<KeyValuePair<string, string>>? query = null, CancellationToken cancellationToken = default)
            where TResponse : class
        {
            try
            {
                var requestUri = BuildUri(uri, query);
                var pipeline = _pipelineProvider.GetPipeline(GenericHttpClientOptions.ClientName);

                using var response = await pipeline.ExecuteAsync(
                    async ct =>
                    {
                        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
                        var httpResponse = await _httpClient.SendAsync(
                            httpRequest,
                            HttpCompletionOption.ResponseHeadersRead,
                            ct);

                        if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests || (int)httpResponse.StatusCode >= 500)
                        {
                            httpResponse.Dispose();
                            throw new HttpRequestException($"Transient HTTP status code {(int)httpResponse.StatusCode} from {requestUri}.");
                        }

                        return httpResponse;
                    },
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync<TResponse>(responseStream, SerializerOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP GET failed for {Uri}", uri);
                throw;
            }
        }

        private Uri BuildUri(string uri, List<KeyValuePair<string, string>>? query)
        {
            var queryString = GetQuery(query);
            return new Uri($"{_httpClient.BaseAddress?.AbsoluteUri}{uri}{queryString}");
        }

        private static string GetQuery(List<KeyValuePair<string, string>>? query)
        {
            return query == null || query.Count == 0
                ? string.Empty
                : "?" + string.Join("&", query.Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));
        }
    }
}
