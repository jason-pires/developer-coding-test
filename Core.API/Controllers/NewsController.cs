using Application.Interfaces;
using Common.Config;
using Domain.DTO;
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

        /// <summary>
        /// Retorna as N melhores notícias do Hacker News.
        /// </summary>
        /// <param name="n">Quantidade de notícias desejada.</param>
        /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
        /// <returns>Lista com as melhores notícias ordenadas.</returns>
        [HttpGet("top/{n:int}")]
        [ProducesResponseType(typeof(List<StoryDetailDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status499ClientClosedRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetTopNNews(int n, CancellationToken cancellationToken)
        {
            if (n <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid request",
                    detail: "N must be a positive integer.");
            }

            if (n > _options.MaxStoriesPerRequest)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid request",
                    detail: $"N must be less than or equal to {_options.MaxStoriesPerRequest}.");
            }

            var topNews = await _hackerNewsService.GetNSortedStoryDetailsAsync(n, cancellationToken);
            return Ok(topNews);
        }
    }
}
