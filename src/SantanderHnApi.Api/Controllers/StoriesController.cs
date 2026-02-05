using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SantanderHnApi.Application.Interfaces;
using SantanderHnApi.Application.Options;
using SantanderHnApi.Domain.Models;

namespace SantanderHnApi.Api.Controllers;

[ApiController]
[Route("api/stories")]
public sealed class StoriesController(
    IBestStoriesService bestStoriesService,
    IOptions<HackerNewsOptions> options)
    : ControllerBase
{
    private readonly HackerNewsOptions _options = options.Value;

    [HttpGet("best")]
    [ProducesResponseType(typeof(IReadOnlyList<BestStory>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBestStories([FromQuery] int n = 10, CancellationToken cancellationToken = default)
    {
        if (n <= 0)
            return BadRequest("n must be greater than 0.");

        if (n > _options.MaxItems)
            return BadRequest($"n must be less than or equal to {_options.MaxItems}.");

        var stories = await bestStoriesService.GetBestStoriesAsync(n, cancellationToken);
        return Ok(stories);
    }
}
