using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Library.Recommendations;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class SuggestionsControllerTests
{
    private static SuggestionsController MakeController(
        Mock<IRecommendationsService> recSvc,
        Mock<IUserManager> userMgr,
        Mock<ILibraryManager> libMgr,
        Mock<IDtoService> dto,
        Guid userId)
    {
        var c = new SuggestionsController(dto.Object, userMgr.Object, libMgr.Object, recSvc.Object);
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("Jellyfin-UserId", userId.ToString())
        }));
        c.ControllerContext = new ControllerContext { HttpContext = ctx };
        return c;
    }

    [Fact]
    public async Task GetSuggestions_SingleRecommendableType_DelegatesToService()
    {
        var rec = new Mock<IRecommendationsService>();
        var ranked = new QueryResult<BaseItemDto>(0, 1, new[] { new BaseItemDto { Name = "Ranked" } });
        rec.Setup(r => r.GetRankedItemsAsync(It.IsAny<Guid>(), BaseItemKind.Movie, null, null, 10, false, It.IsAny<DtoOptions>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(ranked);
        var userMgr = new Mock<IUserManager>();
        userMgr.Setup(u => u.GetUserById(It.IsAny<Guid>())).Returns(new User("u", "default", "default"));
        var libMgr = new Mock<ILibraryManager>();
        var dto = new Mock<IDtoService>();
        var userId = Guid.NewGuid();
        var controller = MakeController(rec, userMgr, libMgr, dto, userId);

        var result = await controller.GetSuggestions(userId, mediaType: Array.Empty<MediaType>(), type: new[] { BaseItemKind.Movie }, startIndex: null, limit: 10, enableTotalRecordCount: false);

        // result is ActionResult<QueryResult<BaseItemDto>>; if implementation returns QueryResult directly, it lands in Value; if it wraps in Ok(...), use OkObjectResult.
        QueryResult<BaseItemDto>? value = result.Value;
        if (value is null && result.Result is OkObjectResult ok)
        {
            value = ok.Value as QueryResult<BaseItemDto>;
        }

        Assert.NotNull(value);
        Assert.Single(value!.Items);
        Assert.Equal("Ranked", value.Items[0].Name);
    }

    [Fact]
    public async Task GetSuggestions_MixedTypes_FallsBackToRandom()
    {
        var rec = new Mock<IRecommendationsService>();
        var userMgr = new Mock<IUserManager>();
        userMgr.Setup(u => u.GetUserById(It.IsAny<Guid>())).Returns(new User("u", "default", "default"));
        var libMgr = new Mock<ILibraryManager>();
        libMgr.Setup(l => l.GetItemsResult(It.IsAny<InternalItemsQuery>())).Returns(new QueryResult<BaseItem>(0, 0, Array.Empty<BaseItem>()));
        var dto = new Mock<IDtoService>();
        dto.Setup(d => d.GetBaseItemDtos(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<DtoOptions>(), It.IsAny<User>(), It.IsAny<BaseItem>(), It.IsAny<bool>()))
           .Returns(Array.Empty<BaseItemDto>());
        var userId = Guid.NewGuid();
        var controller = MakeController(rec, userMgr, libMgr, dto, userId);

        var result = await controller.GetSuggestions(userId, mediaType: Array.Empty<MediaType>(), type: new[] { BaseItemKind.Movie, BaseItemKind.Series }, startIndex: null, limit: 10, enableTotalRecordCount: false);

        rec.Verify(r => r.GetRankedItemsAsync(It.IsAny<Guid>(), It.IsAny<BaseItemKind>(), It.IsAny<Guid?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<DtoOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        libMgr.Verify(l => l.GetItemsResult(It.IsAny<InternalItemsQuery>()), Times.Once);
    }
}
