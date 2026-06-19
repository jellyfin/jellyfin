using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Jellyfin.Api.Controllers;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class MoviesControllerTests
{
    private readonly Mock<IUserManager> _mockUserManager = new();
    private readonly Mock<ILibraryManager> _mockLibraryManager = new();
    private readonly Mock<IDtoService> _mockDtoService = new();
    private readonly Mock<IServerConfigurationManager> _mockServerConfigurationManager = new();
    private readonly MoviesController _subject;

    public MoviesControllerTests()
    {
        _subject = new MoviesController(
            _mockUserManager.Object,
            _mockLibraryManager.Object,
            _mockDtoService.Object,
            _mockServerConfigurationManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };
    }

    [Fact]
    public void GetMovieRecommendations_BuildsSimilarQueryFromBaselineGenresAndTags()
    {
        _mockUserManager.Setup(x => x.GetUserById(It.IsAny<Guid>()))
            .Returns(new User("test", "Default", "Default"));
        _mockServerConfigurationManager.Setup(x => x.Configuration)
            .Returns(new ServerConfiguration { EnableExternalContentInSuggestions = false });
        _mockLibraryManager.Setup(x => x.GetPeople(It.IsAny<InternalPeopleQuery>()))
            .Returns(Array.Empty<PersonInfo>());
        _mockDtoService.Setup(x => x.GetBaseItemDtos(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<DtoOptions>(), It.IsAny<User>(), It.IsAny<BaseItem>()))
            .Returns(new List<BaseItemDto> { new() });

        var baseline = new Movie
        {
            Id = Guid.NewGuid(),
            Name = "Baseline",
            Genres = new[] { "Action", "Thriller" },
            Tags = new[] { "spy" }
        };

        var capturedQueries = new List<InternalItemsQuery>();
        _mockLibraryManager.Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns<InternalItemsQuery>(q =>
            {
                capturedQueries.Add(q);
                if (q.IsPlayed == true)
                {
                    return new List<BaseItem> { baseline };
                }

                if (q.IsMovie == true && q.IsFavoriteOrLiked is null && q.IsPlayed is null)
                {
                    return new List<BaseItem> { new Movie { Id = Guid.NewGuid(), Name = "SimilarResult" } };
                }

                return new List<BaseItem>();
            });

        _subject.GetMovieRecommendations(null, null, Array.Empty<ItemFields>());

        var similarQuery = capturedQueries.Single(q => q.IsMovie == true && q.IsFavoriteOrLiked is null && q.IsPlayed is null);

        Assert.Equal(baseline.Genres, similarQuery.Genres);
        Assert.Equal(baseline.Tags, similarQuery.Tags);
        Assert.Contains(baseline.Id, similarQuery.ExcludeItemIds);
        Assert.Contains(similarQuery.OrderBy, o => o.OrderBy == ItemSortBy.Random);
    }
}
