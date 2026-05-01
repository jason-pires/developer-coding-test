using Application.Interfaces;
using Common.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly IHackerNewsService _hackerNewsService;
        private readonly HackerNewsOptions _options;

        public NewsController(IHackerNewsService hackerNewsService, IOptions<HackerNewsOptions> options)
        {
            _hackerNewsService = hackerNewsService;
            _options = options.Value;
        }

        [HttpGet("top/{n}")]
        public async Task<IActionResult> GetTopNNews(int n, CancellationToken cancellationToken)
        {
            if (n <= 0)
            {
                return BadRequest("N must be a positive integer.");
            }

            if (n > _options.MaxStoriesPerRequest)
            {
                return BadRequest($"N must be less than or equal to {_options.MaxStoriesPerRequest}.");
            }

            try
            {
                var topNews = await _hackerNewsService.GetNSortedStoryDetailsAsync(n, cancellationToken);
                return Ok(topNews);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while fetching the news.");
            }
        }
    }
}
