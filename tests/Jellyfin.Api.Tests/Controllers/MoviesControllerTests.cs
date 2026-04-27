using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Api.Controllers;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class MoviesControllerTests
{
    // Regression test: GetSimilarTo previously built an InternalItemsQuery that did
    // not reference the seed item, so every seed produced the same recommendations.
    [Fact]
    public void GetSimilarTo_PropagatesSeedMetadataIntoQuery()
    {
        var libraryManager = new Mock<ILibraryManager>();
        var dtoService = new Mock<IDtoService>();
        var serverConfig = new Mock<IServerConfigurationManager>();
        var userManager = new Mock<IUserManager>();

        serverConfig.SetupGet(c => c.Configuration)
            .Returns(new ServerConfiguration { EnableExternalContentInSuggestions = false });

        var capturedQueries = new List<InternalItemsQuery>();
        libraryManager
            .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Callback<InternalItemsQuery>(q => capturedQueries.Add(q))
            .Returns(new List<BaseItem>());

        var controller = new MoviesController(
            userManager.Object,
            libraryManager.Object,
            dtoService.Object,
            serverConfig.Object);

        var seedA = new Movie
        {
            Id = Guid.NewGuid(),
            Name = "Mannequin",
            Genres = new[] { "Comedy", "Romance" },
            Tags = new[] { "1980s" }
        };
        var seedB = new Movie
        {
            Id = Guid.NewGuid(),
            Name = "Sin City",
            Genres = new[] { "Crime", "Thriller" },
            Tags = new[] { "noir", "graphic-novel" }
        };

        // Materialize: GetSimilarTo uses yield return, so the queries only fire on enumeration.
        controller.GetSimilarTo(
            user: null,
            baselineItems: new BaseItem[] { seedA, seedB },
            itemLimit: 8,
            dtoOptions: new DtoOptions(),
            type: RecommendationType.SimilarToRecentlyPlayed).ToList();

        Assert.Equal(2, capturedQueries.Count);

        AssertQueryMatchesSeed(capturedQueries[0], seedA);
        AssertQueryMatchesSeed(capturedQueries[1], seedB);

        // The core regression: dissimilar seeds must not produce identical queries.
        Assert.NotEqual(capturedQueries[0].Genres, capturedQueries[1].Genres);
        Assert.NotEqual(capturedQueries[0].Tags, capturedQueries[1].Tags);
        Assert.NotEqual(capturedQueries[0].ExcludeItemIds, capturedQueries[1].ExcludeItemIds);
    }

    private static void AssertQueryMatchesSeed(InternalItemsQuery query, BaseItem seed)
    {
        Assert.Equal(seed.Genres, query.Genres);
        Assert.Equal(seed.Tags, query.Tags);
        Assert.Contains(seed.Id, query.ExcludeItemIds);
        Assert.Contains(query.OrderBy, o => o.OrderBy == ItemSortBy.Random);
    }
}
