using Common.Config;
using Infrastructure.Clients;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using System.Net;
using System.Text;

namespace UnitTests.Infrastructure.Clients;

public class GenericHttpServiceTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(responder(request));
        }
    }

    private sealed record Sample(string Name, int Value);

    private static GenericHttpService BuildSut(StubHandler handler, out IHttpClientFactory factory)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.test") };
        factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(GenericHttpClientOptions.ClientName).Returns(client);

        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder(GenericHttpClientOptions.ClientName, (builder, _) => builder.AddTimeout(TimeSpan.FromSeconds(5)));

        return new GenericHttpService(
            Options.Create(new GenericHttpClientOptions { BaseUrl = "https://example.test" }),
            Substitute.For<ILogger<GenericHttpService>>(),
            factory,
            registry);
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task GetAsync_DeserializesSuccessResponse()
    {
        var handler = new StubHandler(_ => Json("{\"name\":\"abc\",\"value\":7}"));
        var sut = BuildSut(handler, out _);

        var result = await sut.GetAsync<Sample>("/path");

        Assert.NotNull(result);
        Assert.Equal("abc", result!.Name);
        Assert.Equal(7, result.Value);
    }

    [Fact]
    public async Task GetAsync_AppendsQueryString()
    {
        var handler = new StubHandler(_ => Json("{\"name\":\"a\",\"value\":1}"));
        var sut = BuildSut(handler, out _);

        await sut.GetAsync<Sample>("/path", new List<KeyValuePair<string, string>>
        {
            new("k1", "v 1"),
            new("k2", "v&2")
        });

        var uri = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("k1=v+1", uri);
        Assert.Contains("k2=v%262", uri);
    }

    [Fact]
    public async Task GetAsync_ThrowsOnNon2xx()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = BuildSut(handler, out _);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetAsync<Sample>("/missing"));
    }

    [Fact]
    public async Task GetAsync_Throws_OnTransientStatus()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = BuildSut(handler, out _);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetAsync<Sample>("/oops"));
    }
}
