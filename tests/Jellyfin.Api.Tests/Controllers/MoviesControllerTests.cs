using System;
using System.Collections.Generic;
using System.Security.Claims;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class MoviesControllerTests
{
    [Fact]
    public void GetRandomMovies_WithMovies_ReturnsOk()
    {
        var movie = new Movie { Id = Guid.NewGuid(), Name = "Movie" };
        var queryResult = new QueryResult<BaseItem>(new List<BaseItem> { movie });
        var mockLibraryManager = new Mock<ILibraryManager>();
        mockLibraryManager
            .Setup(m => m.GetItemsResult(It.IsAny<InternalItemsQuery>()))
            .Returns(queryResult);

        var mockDtoService = new Mock<IDtoService>();
        mockDtoService
            .Setup(d => d.GetBaseItemDtos(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<DtoOptions>(), null))
            .Returns(new List<BaseItemDto> { new() { Id = movie.Id } });

        var controller = CreateController(mockLibraryManager.Object, mockDtoService.Object);

        var result = controller.GetRandomMovies(null, null, null, false);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public void GetRandomMovies_NoMovies_ReturnsNotFound()
    {
        var mockLibraryManager = new Mock<ILibraryManager>();
        mockLibraryManager
            .Setup(m => m.GetItemsResult(It.IsAny<InternalItemsQuery>()))
            .Returns(new QueryResult<BaseItem>());

        var mockDtoService = new Mock<IDtoService>();
        mockDtoService
            .Setup(d => d.GetBaseItemDtos(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<DtoOptions>(), null))
            .Returns(Array.Empty<BaseItemDto>());

        var controller = CreateController(mockLibraryManager.Object, mockDtoService.Object);

        var result = controller.GetRandomMovies(null, null, null, false);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static MoviesController CreateController(ILibraryManager libraryManager, IDtoService dtoService)
    {
        var mockUserManager = Mock.Of<IUserManager>();
        var mockConfigManager = new Mock<IServerConfigurationManager>();
        mockConfigManager.SetupGet(m => m.Configuration).Returns(new ServerConfiguration());
        var controller = new MoviesController(mockUserManager, libraryManager, dtoService, mockConfigManager.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(InternalClaimTypes.UserId, Guid.NewGuid().ToString("N"))
                }))
            }
        };
        return controller;
    }
}

