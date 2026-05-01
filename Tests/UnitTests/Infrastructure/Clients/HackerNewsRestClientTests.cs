using Domain.Responses;
using Infrastructure.Clients;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace UnitTests.Infrastructure.Clients;

public class HackerNewsRestClientTests
{
    private readonly ILogger<HackerNewsRestClient> _logger = Substitute.For<ILogger<HackerNewsRestClient>>();
    private readonly IGenericHttpService _http = Substitute.For<IGenericHttpService>();

    private HackerNewsRestClient CreateSut() => new(_logger, _http);

    [Fact]
    public async Task GetStoryDetailByIdAsync_CallsCorrectEndpoint()
    {
        var expected = new BestStoryResponse { Id = 42, Title = "x" };
        _http.GetAsync<BestStoryResponse>("/item/42.json", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await CreateSut().GetStoryDetailByIdAsync(42);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetStoryDetailByIdAsync_RethrowsAndLogs_OnError()
    {
        _http.GetAsync<BestStoryResponse>(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("boom"));

        await Assert.ThrowsAsync<HttpRequestException>(() => CreateSut().GetStoryDetailByIdAsync(1));
    }

    [Fact]
    public async Task GetBestStoriesIdsAsync_ReturnsList_WhenSuccess()
    {
        _http.GetAsync<List<int>>("/beststories.json", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new List<int> { 1, 2, 3 });

        var result = await CreateSut().GetBestStoriesIdsAsync();

        Assert.Equal(new[] { 1, 2, 3 }, result);
    }

    [Fact]
    public async Task GetBestStoriesIdsAsync_ReturnsEmpty_WhenNull()
    {
        _http.GetAsync<List<int>>("/beststories.json", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((List<int>?)null);

        var result = await CreateSut().GetBestStoriesIdsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBestStoriesIdsAsync_RethrowsAndLogs_OnError()
    {
        _http.GetAsync<List<int>>(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("boom"));

        await Assert.ThrowsAsync<HttpRequestException>(() => CreateSut().GetBestStoriesIdsAsync());
    }
}
