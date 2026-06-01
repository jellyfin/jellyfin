using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library.Recommendations;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

/// <summary>
/// Tests for <see cref="MoviesController"/>.
/// </summary>
public class MoviesControllerTests
{
    private static MoviesController MakeController(Mock<IRecommendationsService> svc, Guid userId)
    {
        var controller = new MoviesController(svc.Object);
        var httpContext = new DefaultHttpContext();
        // InternalClaimTypes.UserId == "Jellyfin-UserId" (verified in RequestHelpers / ClaimsPrincipalExtensions)
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("Jellyfin-UserId", userId.ToString())
        }));
        httpContext.User = claims;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    /// <summary>
    /// When the service returns a non-empty list the controller should return 200 OK with that list.
    /// </summary>
    [Fact]
    public async Task GetMovieRecommendations_DelegatesToService_ReturnsOkWithCategories()
    {
        var svc = new Mock<IRecommendationsService>();
        var userId = Guid.NewGuid();
        var expected = new List<RecommendationDto>
        {
            new() { BaselineItemName = "Inception", RecommendationType = RecommendationType.SimilarToRecentlyPlayed, Items = Array.Empty<BaseItemDto>() }
        };
        svc.Setup(s => s.GetRecommendationsAsync(
                It.Is<RecommendationRequest>(r => r.Kind == BaseItemKind.Movie),
                It.IsAny<CancellationToken>()))
           .ReturnsAsync(expected);
        var controller = MakeController(svc, userId);

        var result = await controller.GetMovieRecommendations(userId, parentId: null, fields: Array.Empty<ItemFields>(), categoryLimit: 5, itemLimit: 8, CancellationToken.None);

        // BaseJellyfinApiController.Ok<T> returns OkResult<T> which inherits OkObjectResult
        var ok = Assert.IsAssignableFrom<OkObjectResult>(result.Result);
        var dto = Assert.IsAssignableFrom<IEnumerable<RecommendationDto>>(ok.Value);
        Assert.Single(dto);
    }

    /// <summary>
    /// When the user has no history (cold start) the service returns empty; controller should return 200 OK with empty.
    /// </summary>
    [Fact]
    public async Task GetMovieRecommendations_ColdStart_ReturnsOkWithEmpty()
    {
        var svc = new Mock<IRecommendationsService>();
        var userId = Guid.NewGuid();
        svc.Setup(s => s.GetRecommendationsAsync(It.IsAny<RecommendationRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(Array.Empty<RecommendationDto>());
        var controller = MakeController(svc, userId);

        var result = await controller.GetMovieRecommendations(userId, parentId: null, fields: Array.Empty<ItemFields>(), categoryLimit: 5, itemLimit: 8, CancellationToken.None);

        // BaseJellyfinApiController.Ok<T> returns OkResult<T> which inherits OkObjectResult
        var ok = Assert.IsAssignableFrom<OkObjectResult>(result.Result);
        var dto = Assert.IsAssignableFrom<IEnumerable<RecommendationDto>>(ok.Value);
        Assert.Empty(dto);
    }
}
