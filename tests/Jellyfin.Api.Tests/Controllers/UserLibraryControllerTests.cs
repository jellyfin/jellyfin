using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class UserLibraryControllerTests
{
    [Theory]
    [InlineData(new[] { "SCANDAL" }, "Shunkan Sentimental", "SCANDAL - Shunkan Sentimental")]
    [InlineData(new[] { "X", "Y" }, "Track", "X & Y - Track")]
    [InlineData(new[] { "X", "Y", "Z" }, "Track", "X, Y & Z - Track")]
    [InlineData(new[] { "W", "X", "Y", "Z" }, "Track", "W, X, Y & Z - Track")]
    [InlineData(new[] { "X feat. Y" }, "Track", "X feat. Y - Track")]
    [InlineData(new[] { "X vs. Y" }, "Track", "X vs. Y - Track")]
    [InlineData(new[] { "X; Y" }, "Track", "X & Y - Track")]
    [InlineData(new string[0], "Track", "Track")]
    [InlineData(new[] { " " }, "Track", "Track")]
    [InlineData(new[] { "SCANDAL" }, "scandal - Track", "SCANDAL - scandal - Track")]
    [InlineData(new[] { "X", "Y" }, "X & Y - Track", "X & Y - X & Y - Track")]
    [InlineData(new[] { "X", "Y" }, "X feat. Y - Track", "X & Y - X feat. Y - Track")]
    [InlineData(new[] { "Air" }, "Airwaves", "Air - Airwaves")]
    [InlineData(new[] { "Air" }, "AIR - Track", "Air - AIR - Track")]
    [InlineData(new[] { "AC/DC" }, "AC/DC - Track", "AC/DC - AC/DC - Track")]
    [InlineData(new[] { "Artist (Live)" }, "Artist (Live) - Track", "Artist (Live) - Artist (Live) - Track")]
    [InlineData(new[] { " X ; ; Y; " }, "Track", "X & Y - Track")]
    [InlineData(new[] { "", " ", "; ;" }, "Track", "Track")]
    [InlineData(new[] { " X ", "", " Y " }, "Track", "X & Y - Track")]
    [InlineData(new[] { "X; Y; Z;" }, "Track", "X, Y & Z - Track")]
    [InlineData(new[] { "W; X", "Y; Z" }, "Track", "W, X, Y & Z - Track")]
    [InlineData(new[] { "X & Y" }, "Track", "X & Y - Track")]
    [InlineData(new[] { "X feat. Y; Z" }, "Track", "X feat. Y & Z - Track")]
    [InlineData(new[] { "X; Y" }, "X & Y - Track", "X & Y - X & Y - Track")]
    [InlineData(new[] { "X; Y" }, "X; Y - Track", "X & Y - X; Y - Track")]
    [InlineData(new[] { "Live" }, "Live Forever", "Live - Live Forever")]
    [InlineData(new[] { "Artist X" }, "Artist X - Track", "Artist X - Artist X - Track")]
    [InlineData(new[] { "Artist X" }, "Artist X feat. Artist Y - Track", "Artist X - Artist X feat. Artist Y - Track")]
    [InlineData(new[] { "X" }, "  Track; feat. Y  ", "X -   Track; feat. Y  ")]
    [InlineData(new[] { "X" }, "", "X - ")]
    public void GetLatestMedia_MusicVideo_FormatsResponseOnly(string[] artists, string name, string expected)
    {
        var item = new MusicVideo { Name = name, Artists = artists };
        var result = GetLatestMedia(item);

        Assert.Equal(expected, result.Name);
        Assert.Equal(name, item.Name);
        Assert.Equal(artists, item.Artists);
    }

    [Fact]
    public void GetLatestMedia_OtherMedia_PreservesName()
    {
        BaseItem[] items =
        [
            new Movie { Name = "Track" },
            new Episode { Name = "Track" },
            new Audio { Name = "Track", Artists = ["X; Y"] }
        ];
        Assert.All(items, item => Assert.Equal("Track", GetLatestMedia(item).Name));
    }

    [Fact]
    public async Task GetItem_MusicVideo_PreservesName()
    {
        var item = new MusicVideo { Id = Guid.NewGuid(), Name = "Track", Artists = ["Artist"] };
        var response = await CreateController(item).GetItem(null, item.Id);

        Assert.Equal("Track", response.Value!.Name);
    }

    private static BaseItemDto GetLatestMedia(BaseItem item)
    {
        var response = CreateController(item).GetLatestMedia(null, null, [], [], null, null, null, [], null);
        var result = Assert.IsAssignableFrom<OkObjectResult>(response.Result);
        return Assert.Single(Assert.IsAssignableFrom<IEnumerable<BaseItemDto>>(result.Value));
    }

    private static UserLibraryController CreateController(BaseItem item)
    {
        var user = new User("test", "test", "test");
        var userManager = new Mock<IUserManager>();
        userManager.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(user);
        var viewManager = new Mock<IUserViewManager>();
        viewManager.Setup(m => m.GetLatestItems(It.IsAny<LatestItemsQuery>(), It.IsAny<DtoOptions>()))
            .Returns(new List<Tuple<BaseItem, List<BaseItem>>> { new(null!, new List<BaseItem> { item }) });
        var dtoService = new Mock<IDtoService>();
        dtoService.Setup(m => m.GetBaseItemDtos(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<DtoOptions>(), user, null, true))
            .Returns(new[] { new BaseItemDto { Name = item.Name } });
        dtoService.Setup(m => m.GetBaseItemDto(item, It.IsAny<DtoOptions>(), user, null))
            .Returns(() => new BaseItemDto { Name = item.Name });
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(m => m.GetItemById<BaseItem>(item.Id, user)).Returns(item);
        return new UserLibraryController(
            userManager.Object,
            Mock.Of<IUserDataManager>(),
            libraryManager.Object,
            dtoService.Object,
            viewManager.Object,
            Mock.Of<IFileSystem>(),
            Mock.Of<IProviderManager>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }
}
