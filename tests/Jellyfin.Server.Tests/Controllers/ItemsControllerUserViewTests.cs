using System;
using System.Security.Claims;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Controllers;

public sealed class ItemsControllerUserViewTests
{
    private sealed class CapturingUserView : MediaBrowser.Controller.Entities.UserView
    {
        public Guid CapturedParentId { get; private set; }

        protected override QueryResult<MediaBrowser.Controller.Entities.BaseItem> GetItemsInternal(MediaBrowser.Controller.Entities.InternalItemsQuery query)
        {
            CapturedParentId = query.ParentId;
            return new QueryResult<MediaBrowser.Controller.Entities.BaseItem>(Array.Empty<MediaBrowser.Controller.Entities.BaseItem>());
        }
    }

    [Fact]
    public void GetItems_WhenParentIsUserView_DoesNotForceParentIdToUserViewId()
    {
        var userManager = new Mock<IUserManager>(MockBehavior.Strict);
        var libraryManager = new Mock<ILibraryManager>(MockBehavior.Strict);
        var localization = new Mock<ILocalizationManager>(MockBehavior.Strict);
        var dtoService = new Mock<IDtoService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<ItemsController>>(MockBehavior.Loose);
        var sessionManager = new Mock<ISessionManager>(MockBehavior.Strict);
        var userDataManager = new Mock<IUserDataManager>(MockBehavior.Strict);

        var viewId = Guid.NewGuid();
        var view = new CapturingUserView { Id = viewId };

        libraryManager
            .Setup(m => m.GetParentItem(viewId, It.IsAny<Guid?>()))
            .Returns(view);

        dtoService
            .Setup(m => m.GetBaseItemDtos(
                It.IsAny<MediaBrowser.Controller.Entities.BaseItem[]>(),
                It.IsAny<DtoOptions>(),
                It.IsAny<User>()))
            .Returns(Array.Empty<BaseItemDto>());

        var controller = new ItemsController(
            userManager.Object,
            libraryManager.Object,
            localization.Object,
            dtoService.Object,
            logger.Object,
            sessionManager.Object,
            userDataManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[]
                            {
                                new Claim(InternalClaimTypes.IsApiKey, "true")
                            },
                            "TestAuth"))
                }
            }
        };

        _ = controller.GetItems(
            userId: null,
            maxOfficialRating: null,
            hasThemeSong: null,
            hasThemeVideo: null,
            hasSubtitles: null,
            hasSpecialFeature: null,
            hasTrailer: null,
            adjacentTo: null,
            indexNumber: null,
            parentIndexNumber: null,
            hasParentalRating: null,
            isHd: null,
            is4K: null,
            locationTypes: Array.Empty<MediaBrowser.Model.Entities.LocationType>(),
            excludeLocationTypes: Array.Empty<MediaBrowser.Model.Entities.LocationType>(),
            isMissing: null,
            isUnaired: null,
            minCommunityRating: null,
            minCriticRating: null,
            minPremiereDate: null,
            minDateLastSaved: null,
            minDateLastSavedForUser: null,
            maxPremiereDate: null,
            hasOverview: null,
            hasImdbId: null,
            hasTmdbId: null,
            hasTvdbId: null,
            isMovie: null,
            isSeries: null,
            isNews: null,
            isKids: null,
            isSports: null,
            excludeItemIds: Array.Empty<Guid>(),
            startIndex: null,
            limit: null,
            recursive: true,
            searchTerm: null,
            sortOrder: Array.Empty<SortOrder>(),
            parentId: viewId,
            fields: Array.Empty<ItemFields>(),
            excludeItemTypes: Array.Empty<Jellyfin.Data.Enums.BaseItemKind>(),
            includeItemTypes: Array.Empty<Jellyfin.Data.Enums.BaseItemKind>(),
            filters: Array.Empty<Jellyfin.Data.Enums.ItemFilter>(),
            isFavorite: null,
            mediaTypes: Array.Empty<Jellyfin.Data.Enums.MediaType>(),
            imageTypes: Array.Empty<MediaBrowser.Model.Entities.ImageType>(),
            sortBy: Array.Empty<ItemSortBy>(),
            isPlayed: null,
            genres: Array.Empty<string>(),
            officialRatings: Array.Empty<string>(),
            tags: Array.Empty<string>(),
            years: Array.Empty<int>(),
            enableUserData: null,
            imageTypeLimit: null,
            enableImageTypes: Array.Empty<MediaBrowser.Model.Entities.ImageType>(),
            person: null,
            personIds: Array.Empty<Guid>(),
            personTypes: Array.Empty<string>(),
            studios: Array.Empty<string>(),
            artists: Array.Empty<string>(),
            excludeArtistIds: Array.Empty<Guid>(),
            artistIds: Array.Empty<Guid>(),
            albumArtistIds: Array.Empty<Guid>(),
            contributingArtistIds: Array.Empty<Guid>(),
            albums: Array.Empty<string>(),
            albumIds: Array.Empty<Guid>(),
            ids: Array.Empty<Guid>(),
            videoTypes: Array.Empty<MediaBrowser.Model.Entities.VideoType>(),
            minOfficialRating: null,
            isLocked: null,
            isPlaceHolder: null,
            hasOfficialRating: null,
            collapseBoxSetItems: null,
            minWidth: null,
            minHeight: null,
            maxWidth: null,
            maxHeight: null,
            is3D: null,
            seriesStatus: Array.Empty<Jellyfin.Data.Enums.SeriesStatus>(),
            nameStartsWithOrGreater: null,
            nameStartsWith: null,
            nameLessThan: null,
            studioIds: Array.Empty<Guid>(),
            genreIds: Array.Empty<Guid>(),
            enableTotalRecordCount: true,
            enableImages: true);

        Assert.Equal(Guid.Empty, view.CapturedParentId);
    }
}

