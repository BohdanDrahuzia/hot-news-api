using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using SantanderHnApi.Api.Contracts;
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
        var serviceMock = new Mock<IBestStoriesService>();
        serviceMock.Setup(s => s.GetBestStoriesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<BestStory>());

        var controller = new StoriesController(
            serviceMock.Object,
            Options.Create(new HackerNewsOptions { MaxItems = 10 }));

        var result = await controller.GetBestStories(0, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetBestStories_ReturnsBadRequest_WhenNExceedsMax()
    {
        var serviceMock = new Mock<IBestStoriesService>();
        serviceMock.Setup(s => s.GetBestStoriesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<BestStory>());

        var controller = new StoriesController(
            serviceMock.Object,
            Options.Create(new HackerNewsOptions { MaxItems = 2 }));

        var result = await controller.GetBestStories(3, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetBestStories_ReturnsOk_WhenNIsValid()
    {
        var serviceMock = new Mock<IBestStoriesService>();
        serviceMock.Setup(s => s.GetBestStoriesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<BestStory>());

        var controller = new StoriesController(
            serviceMock.Object,
            Options.Create(new HackerNewsOptions { MaxItems = 10 }));

        var result = await controller.GetBestStories(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<BestStoriesResponse>(ok.Value);
        Assert.Equal(0, payload.Count);
    }
}
