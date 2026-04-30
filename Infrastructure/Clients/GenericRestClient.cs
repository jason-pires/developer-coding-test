using Common.Config;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;

namespace Infrastructure.Clients
{
    public class GenericRestClient : IGenericRestClient
    {
        private readonly ILogger<GenericRestClient> _logger;
        private readonly HttpClient _httpClient;
        
        public GenericRestClient(
            IOptions<GenericRestClientOptions> options,
            ILogger<GenericRestClient> logger,
            IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("GenericRestClient");
            _httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
            _logger = logger;
        }

        public async Task<TResponse> GetAsync<TResponse>(string uri, List<KeyValuePair<string, string>> query = null) where TResponse : class
        {
            var response = new HttpResponseMessage();
            var responseBody = string.Empty;
            try
            {
                var queryString = GetQuery(query);
                var httpRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{_httpClient.BaseAddress?.AbsoluteUri}{uri}{queryString}")
                };

                response = await _httpClient.SendAsync(httpRequest);
                responseBody = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();

                return JsonConvert.DeserializeObject<TResponse>(responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RestClient error in {HttpMethod.Get}, Uri: {uri} - Resposta: {responseBody}");
                throw;
            }
        }

        private string GetQuery(List<KeyValuePair<string, string>> query)
        {
            return query == null ? string.Empty :
                "?" + string.Join("&", query.Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));
        }
    }
}
