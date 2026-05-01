using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly IHackerNewsService _hackerNewsService;

        public NewsController(IHackerNewsService hackerNewsService)
        {
            _hackerNewsService = hackerNewsService;
        }

        [HttpGet("top/{n}")]
        public async Task<IActionResult> GetTopNNews(int n)
        {
            if (n <= 0)
            {
                return BadRequest("N must be a positive integer.");
            }
            try
            {
                var topNews = await _hackerNewsService.GetNSortedStoryDetailsAsync(n);
                return Ok(topNews);
            }
            catch (Exception ex)
            {
                // Log the exception (not shown here for brevity)
                return StatusCode(500, "An error occurred while fetching the news.");
            }
        }
    }
}
