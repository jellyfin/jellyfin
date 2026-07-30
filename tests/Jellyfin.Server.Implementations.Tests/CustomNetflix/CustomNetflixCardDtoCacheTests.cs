using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.CustomNetflix;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Dto;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixCardDtoCacheTests
{
    [Fact]
    public void GetBaseItemDtos_HydratesEachItemOnlyOncePerRequest()
    {
        var user = new User("cards", "auth", "reset");
        var firstItem = new Movie { Id = Guid.NewGuid() };
        var sharedItem = new Movie { Id = Guid.NewGuid() };
        var lastItem = new Movie { Id = Guid.NewGuid() };
        var hydratedBatches = new List<Guid[]>();
        var dtoService = new Mock<IDtoService>();
        dtoService
            .Setup(mock => mock.GetBaseItemDtos(
                It.IsAny<IReadOnlyList<BaseItem>>(),
                It.IsAny<DtoOptions>(),
                user,
                null,
                true))
            .Returns((
                IReadOnlyList<BaseItem> items,
                DtoOptions _,
                User? _,
                BaseItem? _,
                bool _) =>
            {
                hydratedBatches.Add(items.Select(item => item.Id).ToArray());
                return items.Select(item => new BaseItemDto { Id = item.Id }).ToArray();
            });
        var cache = new CustomNetflixCardDtoCache(dtoService.Object);

        var first = cache.GetBaseItemDtos([firstItem, sharedItem], user);
        var second = cache.GetBaseItemDtos([sharedItem, lastItem], user);

        Assert.Equal([firstItem.Id, sharedItem.Id], hydratedBatches[0]);
        Assert.Equal([lastItem.Id], hydratedBatches[1]);
        Assert.Same(first[1], second[0]);
        dtoService.VerifyAll();
    }
}
