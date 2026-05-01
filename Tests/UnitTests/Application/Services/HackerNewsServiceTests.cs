using Application.Interfaces;
using Application.Services;
using Common.Config;
using Domain.DTO;
using Domain.Responses;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UnitTests.Application.Services;

public class HackerNewsServiceTests
{
    private readonly ILogger<HackerNewsService> _logger = Substitute.For<ILogger<HackerNewsService>>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IHackerNewsGateway _gateway = Substitute.For<IHackerNewsGateway>();
    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly HackerNewsOptions _options = new()
    {
        MaxStoriesPerRequest = 50,
        MaxConcurrentStoryRequests = 4,
        BestStoriesCacheSeconds = 60,
        StoryDetailsCacheSeconds = 300
    };

    private HackerNewsService CreateSut() =>
        new(_logger, _mapper, _gateway, _cache, Options.Create(_options));

    public HackerNewsServiceTests()
    {
        _cache.GetOrCreateAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<IReadOnlyList<int>>>>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => ((Func<CancellationToken, Task<IReadOnlyList<int>>>)call[1])(CancellationToken.None));

        _cache.GetOrCreateAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<BestStoryResponse?>>>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(call => ((Func<CancellationToken, Task<BestStoryResponse?>>)call[1])(CancellationToken.None));

        _mapper.Map<StoryDetailDTO>(Arg.Any<BestStoryResponse>())
            .Returns(call =>
            {
                var src = (BestStoryResponse)call[0];
                return new StoryDetailDTO { Title = src.Title, Score = src.Score };
            });
    }

    [Fact]
    public async Task ReturnsRequestedNumberOfStories_WhenAvailable()
    {
        _gateway.GetBestStoriesIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<int> { 1, 2, 3, 4, 5 });
        _gateway.GetStoryDetailByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => new BestStoryResponse { Id = (int)call[0], Title = $"t{call[0]}", Score = (int)call[0] });

        var result = await CreateSut().GetNSortedStoryDetailsAsync(3);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task BoundsRequestByMaxStoriesPerRequest()
    {
        _options.MaxStoriesPerRequest = 2;
        _gateway.GetBestStoriesIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<int> { 1, 2, 3, 4, 5 });
        _gateway.GetStoryDetailByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => new BestStoryResponse { Id = (int)call[0], Title = "t", Score = 1 });

        var result = await CreateSut().GetNSortedStoryDetailsAsync(100);

        Assert.Equal(2, result.Count);
        await _gateway.Received(2).GetStoryDetailByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsEmpty_WhenNoStoryIds()
    {
        _gateway.GetBestStoriesIdsAsync(Arg.Any<CancellationToken>()).Returns(new List<int>());

        var result = await CreateSut().GetNSortedStoryDetailsAsync(10);

        Assert.Empty(result);
        await _gateway.DidNotReceive().GetStoryDetailByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FiltersOutNullStoryDetails()
    {
        _gateway.GetBestStoriesIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<int> { 1, 2, 3 });
        _gateway.GetStoryDetailByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new BestStoryResponse { Id = 1, Title = "ok", Score = 1 });
        _gateway.GetStoryDetailByIdAsync(2, Arg.Any<CancellationToken>())
            .Returns((BestStoryResponse?)null);
        _gateway.GetStoryDetailByIdAsync(3, Arg.Any<CancellationToken>())
            .Returns(new BestStoryResponse { Id = 3, Title = "ok", Score = 3 });

        var result = await CreateSut().GetNSortedStoryDetailsAsync(3);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UsesCacheForBestStoriesAndDetails()
    {
        _gateway.GetBestStoriesIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<int> { 7, 8 });
        _gateway.GetStoryDetailByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => new BestStoryResponse { Id = (int)call[0], Title = "x", Score = 1 });

        await CreateSut().GetNSortedStoryDetailsAsync(2);

        await _cache.Received(1).GetOrCreateAsync(
            "hackernews:beststories",
            Arg.Any<Func<CancellationToken, Task<IReadOnlyList<int>>>>(),
            TimeSpan.FromSeconds(_options.BestStoriesCacheSeconds),
            Arg.Any<CancellationToken>());

        await _cache.Received(1).GetOrCreateAsync(
            "hackernews:story:7",
            Arg.Any<Func<CancellationToken, Task<BestStoryResponse?>>>(),
            TimeSpan.FromSeconds(_options.StoryDetailsCacheSeconds),
            Arg.Any<CancellationToken>());
    }
}
