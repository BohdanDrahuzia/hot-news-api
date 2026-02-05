using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SantanderHnApi.Api.Controllers;
using SantanderHnApi.Application.Interfaces;
using SantanderHnApi.Application.Options;
using SantanderHnApi.Domain.Models;
using Xunit;

namespace SantanderHnApi.Tests;

public sealed class StoriesControllerTests
{
    [Fact]
    public async Task GetBestStories_ReturnsBadRequest_WhenNIsZero()
    {
        var controller = new StoriesController(
            new StubBestStoriesService(),
            Options.Create(new HackerNewsOptions { MaxItems = 10 }));

        var result = await controller.GetBestStories(0, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetBestStories_ReturnsBadRequest_WhenNExceedsMax()
    {
        var controller = new StoriesController(
            new StubBestStoriesService(),
            Options.Create(new HackerNewsOptions { MaxItems = 2 }));

        var result = await controller.GetBestStories(3, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetBestStories_ReturnsOk_WhenNIsValid()
    {
        var controller = new StoriesController(
            new StubBestStoriesService(),
            Options.Create(new HackerNewsOptions { MaxItems = 10 }));

        var result = await controller.GetBestStories(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    private sealed class StubBestStoriesService : IBestStoriesService
    {
        public Task<IReadOnlyList<BestStory>> GetBestStoriesAsync(int count, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<BestStory>>(Array.Empty<BestStory>());
    }
}
