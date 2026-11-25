using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using Emby.Server.Implementations.Data;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Tests.Item;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;
using ServerLibraryManager = Emby.Server.Implementations.Library.LibraryManager;

namespace Jellyfin.Server.Implementations.Tests.Library;

public sealed class QueryFiltersTests : SqliteDbTestFixture
{
    private readonly DirectoryInfo _rootDirectory;
    private readonly ItemTypeLookup _itemTypeLookup = new();
    private readonly Mock<IUserViewManager> _userViewManager = new();
    private readonly User _user = new("restricted", "auth", "reset")
    {
        MaxParentalRatingScore = 50
    };

    private readonly Guid _firstLibraryId = Guid.NewGuid();
    private readonly Guid _secondLibraryId = Guid.NewGuid();
    private readonly Guid _inaccessibleLibraryId = Guid.NewGuid();
    private readonly Folder _folder = new() { Id = Guid.NewGuid() };
    private readonly AggregateFolder _aggregateRoot = new() { Id = Guid.NewGuid() };
    private readonly CollectionFolder _collection;
    private readonly UserView _view;
    private readonly UserRootFolder _userRoot;
    private readonly FilterController _controller;

    public QueryFiltersTests()
    {
        _rootDirectory = Directory.CreateTempSubdirectory("jellyfin-query-filters-");
        _user.SetPreference(PreferenceKind.BlockedTags, ["blocked"]);

        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        fixture.Inject<IItemRepository>(CreateBaseItemRepository(_itemTypeLookup));
        fixture.Inject(new Lazy<IUserViewManager>(() => _userViewManager.Object));
        var configuration = fixture.Freeze<Mock<IServerConfigurationManager>>();
        configuration.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        configuration.Setup(c => c.ApplicationPaths.ProgramDataPath).Returns(_rootDirectory.FullName);
        configuration.Setup(c => c.ApplicationPaths.DefaultUserViewsPath).Returns(_rootDirectory.FullName);

        var libraryManager = fixture.Create<ServerLibraryManager>();
        _userRoot = new UserRootFolder
        {
            Id = libraryManager.GetNewItemId(_rootDirectory.FullName, typeof(UserRootFolder)),
            Path = _rootDirectory.FullName
        };
        _collection = new CollectionFolder
        {
            Id = Guid.NewGuid(),
            PhysicalFolderIds = [_firstLibraryId]
        };
        _view = new UserView
        {
            Id = Guid.NewGuid(),
            DisplayParentId = _collection.Id
        };
        var secondCollection = new CollectionFolder
        {
            Id = Guid.NewGuid(),
            PhysicalFolderIds = [_secondLibraryId]
        };
        libraryManager.RegisterItem(_userRoot);
        libraryManager.RegisterItem(_aggregateRoot);
        libraryManager.RegisterItem(_collection);
        libraryManager.RegisterItem(_view);
        libraryManager.RegisterItem(_folder);

        _userViewManager.Setup(m => m.GetUserViews(It.Is<UserViewQuery>(q => q.User == _user && q.IncludeHidden)))
            .Returns([_collection, secondCollection]);
        var userManager = fixture.Freeze<Mock<IUserManager>>();
        userManager.Setup(m => m.GetUserById(_user.Id)).Returns(_user);
        _controller = new FilterController(libraryManager, userManager.Object, Mock.Of<ILocalizationManager>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(InternalClaimTypes.UserId, _user.Id.ToString("N"))]))
                }
            }
        };

        using var context = CreateDbContext();
        foreach (var id in new[] { _firstLibraryId, _secondLibraryId, _inaccessibleLibraryId, _folder.Id, _userRoot.Id, _aggregateRoot.Id })
        {
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = id,
                Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Folder],
                IsFolder = true
            });
        }

        context.SaveChanges();

        SeedItems(BaseItemKind.Movie, 0, "PG", "G", "PG-13");
        SeedItems(BaseItemKind.Series, 1, "TV-PG", "TV-G", "TV-14");
    }

    [Theory]
    [InlineData(BaseItemKind.Movie, null)]
    [InlineData(BaseItemKind.Series, null)]
    [InlineData(BaseItemKind.Movie, "library")]
    [InlineData(BaseItemKind.Series, "library")]
    [InlineData(BaseItemKind.Movie, "view")]
    [InlineData(BaseItemKind.Series, "view")]
    [InlineData(BaseItemKind.Movie, "folder")]
    [InlineData(BaseItemKind.Series, "folder")]
    [InlineData(BaseItemKind.Movie, "userRoot")]
    [InlineData(BaseItemKind.Series, "userRoot")]
    [InlineData(BaseItemKind.Movie, "aggregateRoot")]
    [InlineData(BaseItemKind.Series, "aggregateRoot")]
    public void GetQueryFiltersLegacy_ReturnsAccessibleYearsAndRatings(BaseItemKind itemType, string? parent)
    {
        // A missing userId uses the authenticated user's access restrictions.
        var result = _controller.GetQueryFiltersLegacy(null, GetParentId(parent), [itemType], []);
        var filters = Assert.IsType<QueryFiltersLegacy>(result.Value);

        AssertYearsAndRatings(itemType, parent, filters.Years, filters.OfficialRatings);
    }

    [Theory]
    [InlineData(BaseItemKind.Movie)]
    [InlineData(BaseItemKind.Series)]
    public void GetQueryFiltersLegacy_NoAccessibleLibraries_ReturnsEmptyFilters(BaseItemKind itemType)
    {
        _userViewManager.Setup(m => m.GetUserViews(It.IsAny<UserViewQuery>())).Returns([]);

        var result = _controller.GetQueryFiltersLegacy(null, null, [itemType], []);
        var filters = Assert.IsType<QueryFiltersLegacy>(result.Value);

        Assert.Empty(filters.Years);
        Assert.Empty(filters.OfficialRatings);
        Assert.Empty(filters.Tags);
        Assert.Empty(filters.Genres);
    }

    [Theory]
    [InlineData(BaseItemKind.Movie, null, true)]
    [InlineData(BaseItemKind.Series, null, true)]
    [InlineData(BaseItemKind.Movie, null, false)]
    [InlineData(BaseItemKind.Series, null, false)]
    [InlineData(BaseItemKind.Movie, "library", true)]
    [InlineData(BaseItemKind.Series, "library", true)]
    [InlineData(BaseItemKind.Movie, "library", false)]
    [InlineData(BaseItemKind.Series, "library", false)]
    [InlineData(BaseItemKind.Movie, "view", true)]
    [InlineData(BaseItemKind.Series, "view", true)]
    [InlineData(BaseItemKind.Movie, "view", false)]
    [InlineData(BaseItemKind.Series, "view", false)]
    [InlineData(BaseItemKind.Movie, "folder", true)]
    [InlineData(BaseItemKind.Series, "folder", true)]
    [InlineData(BaseItemKind.Movie, "folder", false)]
    [InlineData(BaseItemKind.Series, "folder", false)]
    [InlineData(BaseItemKind.Movie, "userRoot", true)]
    [InlineData(BaseItemKind.Series, "userRoot", true)]
    [InlineData(BaseItemKind.Movie, "aggregateRoot", true)]
    [InlineData(BaseItemKind.Series, "aggregateRoot", true)]
    public void GetQueryFilters_ReturnsAccessibleYearsAndRatings(BaseItemKind itemType, string? parent, bool recursive)
    {
        var filters = GetFilters(itemType, GetParentId(parent), recursive);

        AssertYearsAndRatings(itemType, parent, filters.Years, filters.OfficialRatings);
    }

    [Theory]
    [InlineData(BaseItemKind.Movie, true)]
    [InlineData(BaseItemKind.Series, true)]
    [InlineData(BaseItemKind.Movie, false)]
    [InlineData(BaseItemKind.Series, false)]
    public void GetQueryFilters_Recursive_ControlsWhetherDescendantsContribute(BaseItemKind itemType, bool recursive)
    {
        using var context = CreateDbContext();
        var nestedFolder = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Folder],
            IsFolder = true,
            ParentId = _folder.Id,
            TopParentId = _firstLibraryId
        };
        context.BaseItems.Add(nestedFolder);
        context.SaveChanges();
        var descendantId = AddItem(itemType, _firstLibraryId, nestedFolder.Id, 2019, "Descendant");
        context.AncestorIds.Add(new AncestorId
        {
            ItemId = descendantId,
            Item = context.BaseItems.Find(descendantId)!,
            ParentItemId = _folder.Id,
            ParentItem = context.BaseItems.Find(_folder.Id)!
        });
        context.SaveChanges();

        var filters = GetFilters(itemType, _folder.Id, recursive);
        var firstYear = itemType == BaseItemKind.Movie ? 2001 : 2002;
        var firstRating = itemType == BaseItemKind.Movie ? "PG" : "TV-PG";

        Assert.Equal(recursive ? [firstYear, 2019] : new[] { firstYear }, filters.Years);
        Assert.Equal(recursive ? ["Descendant", firstRating] : new[] { firstRating }, filters.OfficialRatings);
    }

    [Theory]
    [InlineData(BaseItemKind.Movie, false)]
    [InlineData(BaseItemKind.Series, false)]
    [InlineData(BaseItemKind.Movie, true)]
    [InlineData(BaseItemKind.Series, true)]
    public void GetQueryFilters_MissingMetadata_DoesNotExcludeTheOtherField(BaseItemKind itemType, bool withParent)
    {
        using (var context = CreateDbContext())
        {
            foreach (var item in context.BaseItems)
            {
                item.ProductionYear = null;
                item.OfficialRating = null;
            }

            context.SaveChanges();
        }

        AddItem(itemType, _firstLibraryId, _folder.Id, null, "PG");
        AddItem(itemType, _firstLibraryId, _folder.Id, 2019, null);
        AddItem(itemType, _firstLibraryId, _folder.Id, 2019, string.Empty);
        AddItem(itemType, _firstLibraryId, _folder.Id, -1, "PG");

        var filters = GetFilters(itemType, withParent ? _collection.Id : null);

        Assert.Equal([2019], filters.Years);
        Assert.Equal(["PG"], filters.OfficialRatings);
    }

    [Theory]
    [InlineData(BaseItemKind.Movie, false)]
    [InlineData(BaseItemKind.Series, false)]
    [InlineData(BaseItemKind.Movie, true)]
    [InlineData(BaseItemKind.Series, true)]
    public void GetQueryFilters_AllowedTags_OnlyIncludesAllowedItems(BaseItemKind itemType, bool withParent)
    {
        _user.SetPreference(PreferenceKind.AllowedTags, ["allowed"]);
        var allowedId = AddItem(itemType, _firstLibraryId, _folder.Id, 2017, "Allowed");
        AddItemValue(allowedId, ItemValueType.Tags, "allowed");
        var blockedId = AddItem(itemType, _firstLibraryId, _folder.Id, 2018, "Blocked", blockedTag: true);
        AddItemValue(blockedId, ItemValueType.Tags, "allowed");

        var filters = GetFilters(itemType, withParent ? _collection.Id : null);

        Assert.Equal([2017], filters.Years);
        Assert.Equal(["Allowed"], filters.OfficialRatings);
        Assert.Equal(["allowed"], filters.Tags);
    }

    [Theory]
    [InlineData(BaseItemKind.Movie)]
    [InlineData(BaseItemKind.Series)]
    public void GetQueryFilters_NoAccessibleLibraries_ReturnsEmptyFilters(BaseItemKind itemType)
    {
        _userViewManager.Setup(m => m.GetUserViews(It.IsAny<UserViewQuery>())).Returns([]);

        var filters = GetFilters(itemType);

        Assert.Empty(filters.Years);
        Assert.Empty(filters.OfficialRatings);
        Assert.Empty(filters.Genres);
        Assert.Empty(filters.Tags);
        Assert.Empty(filters.AudioLanguages);
        Assert.Empty(filters.SubtitleLanguages);
    }

    [Theory]
    [InlineData(BaseItemKind.Movie, false)]
    [InlineData(BaseItemKind.Series, false)]
    [InlineData(BaseItemKind.Movie, true)]
    [InlineData(BaseItemKind.Series, true)]
    public void GetQueryFilters_IsMovie_UsesTheSameFilterForMetadata(BaseItemKind itemType, bool withParent)
    {
        var result = _controller.GetQueryFilters(
            userId: null,
            parentId: withParent ? _collection.Id : null,
            includeItemTypes: [itemType],
            isAiring: null,
            isMovie: itemType != BaseItemKind.Movie,
            isSports: null,
            isKids: null,
            isNews: null,
            isSeries: null,
            recursive: null);
        var filters = Assert.IsType<QueryFilters>(result.Value);

        Assert.Empty(filters.Years);
        Assert.Empty(filters.OfficialRatings);
    }

    [Theory]
    [InlineData(BaseItemKind.Movie, false)]
    [InlineData(BaseItemKind.Series, false)]
    [InlineData(BaseItemKind.Movie, true)]
    [InlineData(BaseItemKind.Series, true)]
    public void GetQueryFilters_PreservesGenresTagsAndLanguages(BaseItemKind itemType, bool withParent)
    {
        var isMovie = itemType == BaseItemKind.Movie;
        var visibleId = AddItem(itemType, _firstLibraryId, _folder.Id, isMovie ? 2001 : 2002, isMovie ? "PG" : "TV-PG");
        AddItemValue(visibleId, ItemValueType.Tags, "visible");
        AddItemValue(visibleId, ItemValueType.Genre, "Drama");
        var hiddenId = AddItem(itemType, _inaccessibleLibraryId, _inaccessibleLibraryId, 2099, "Hidden");
        AddItemValue(hiddenId, ItemValueType.Tags, "hidden");
        AddItemValue(hiddenId, ItemValueType.Genre, "Horror");

        // Languages come from episodes; years and ratings come from the series.
        if (!isMovie)
        {
            visibleId = AddItem(BaseItemKind.Episode, _firstLibraryId, visibleId, 2088, "Episode rating");
            hiddenId = AddItem(BaseItemKind.Episode, _inaccessibleLibraryId, hiddenId, 2089, "Hidden episode rating");
        }

        AddLanguages(visibleId, "eng", "spa");
        AddLanguages(hiddenId, "ger", "fre");
        using (var context = CreateDbContext())
        {
            foreach (var name in new[] { "Drama", "Horror" })
            {
                var id = Guid.NewGuid();
                context.BaseItems.Add(new BaseItemEntity
                {
                    Id = id,
                    Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Genre],
                    Name = name,
                    CleanName = name.ToLowerInvariant(),
                    PresentationUniqueKey = id.ToString("N")
                });
            }

            context.SaveChanges();
        }

        var filters = GetFilters(itemType, withParent ? _collection.Id : null);

        Assert.Equal(["Drama"], filters.Genres.Select(g => g.Name));
        Assert.NotEqual(Guid.Empty, Assert.Single(filters.Genres).Id);
        Assert.Equal(["visible"], filters.Tags);
        var audioLanguage = Assert.Single(filters.AudioLanguages);
        Assert.Equal("eng", audioLanguage.Value);
        Assert.Equal("eng", audioLanguage.Name);
        var subtitleLanguage = Assert.Single(filters.SubtitleLanguages);
        Assert.Equal("spa", subtitleLanguage.Value);
        Assert.Equal("spa", subtitleLanguage.Name);
        AssertYearsAndRatings(itemType, withParent ? "library" : null, filters.Years, filters.OfficialRatings);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _rootDirectory.Delete(true);
        }
    }

    private QueryFilters GetFilters(BaseItemKind itemType, Guid? parentId = null, bool? recursive = null)
    {
        var result = _controller.GetQueryFilters(
            userId: null,
            parentId: parentId,
            includeItemTypes: [itemType],
            isAiring: null,
            isMovie: null,
            isSports: null,
            isKids: null,
            isNews: null,
            isSeries: null,
            recursive: recursive);
        return Assert.IsType<QueryFilters>(result.Value);
    }

    private Guid? GetParentId(string? parent)
    {
        return parent switch
        {
            null => null,
            "library" => _collection.Id,
            "view" => _view.Id,
            "folder" => _folder.Id,
            "userRoot" => _userRoot.Id,
            "aggregateRoot" => _aggregateRoot.Id,
            _ => throw new ArgumentOutOfRangeException(nameof(parent))
        };
    }

    private static void AssertYearsAndRatings(BaseItemKind itemType, string? parent, IReadOnlyList<int> years, IReadOnlyList<string> officialRatings)
    {
        var offset = itemType == BaseItemKind.Movie ? 0 : 1;
        int[] expectedYears = parent switch
        {
            "folder" => [2001 + offset],
            "library" or "view" => [2001 + offset, 2005 + offset],
            _ => [2001 + offset, 2005 + offset, 2011 + offset]
        };
        string[] ratings = itemType == BaseItemKind.Movie ? ["PG", "G", "PG-13"] : ["TV-PG", "TV-G", "TV-14"];

        Assert.Equal(expectedYears, years);
        Assert.Equal(ratings.Take(expectedYears.Length).Order(StringComparer.Ordinal), officialRatings);
    }

    private void SeedItems(BaseItemKind kind, int yearOffset, string firstRating, string siblingRating, string secondRating)
    {
        // Unsorted, duplicate, and missing values exercise metadata aggregation.
        AddItem(kind, _secondLibraryId, _secondLibraryId, 2011 + yearOffset, secondRating);
        AddItem(kind, _firstLibraryId, _firstLibraryId, 2005 + yearOffset, siblingRating);
        AddItem(kind, _firstLibraryId, _folder.Id, 2001 + yearOffset, firstRating);
        AddItem(kind, _firstLibraryId, _folder.Id, 2001 + yearOffset, firstRating);
        AddItem(kind, _firstLibraryId, _folder.Id, null, null);
        AddItem(kind, _firstLibraryId, _folder.Id, 0, string.Empty);

        // Distinct metadata reveals any restricted items in the response.
        AddItem(kind, _inaccessibleLibraryId, _inaccessibleLibraryId, 1991 + yearOffset, "Inaccessible library");
        AddItem(kind, _firstLibraryId, _folder.Id, 2021 + yearOffset, "Above parental limit", parentalRating: 100);
        AddItem(kind, _firstLibraryId, _folder.Id, 2031 + yearOffset, "Blocked tag", blockedTag: true);
    }

    private Guid AddItem(BaseItemKind kind, Guid libraryId, Guid parentId, int? year, string? rating, int parentalRating = 10, bool blockedTag = false)
    {
        using var context = CreateDbContext();
        var item = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = _itemTypeLookup.BaseItemKindNames[kind],
            MediaType = "Video",
            IsFolder = kind == BaseItemKind.Series,
            IsMovie = kind == BaseItemKind.Movie,
            IsVirtualItem = false,
            TopParentId = libraryId,
            ParentId = parentId,
            ProductionYear = year,
            OfficialRating = rating,
            InheritedParentalRatingValue = parentalRating
        };
        context.BaseItems.Add(item);

        // Virtual roots and collection views are not stored ancestors.
        foreach (var ancestorId in new[] { libraryId, parentId }.Distinct())
        {
            context.AncestorIds.Add(new AncestorId
            {
                ItemId = item.Id,
                Item = item,
                ParentItemId = ancestorId,
                ParentItem = context.BaseItems.Find(ancestorId)!
            });
        }

        context.SaveChanges();
        if (blockedTag)
        {
            AddItemValue(item.Id, ItemValueType.Tags, "blocked");
        }

        return item.Id;
    }

    private void AddItemValue(Guid itemId, ItemValueType type, string value)
    {
        using var context = CreateDbContext();
        var itemValue = context.ItemValues.SingleOrDefault(t => t.Type == type && t.Value == value);
        if (itemValue is null)
        {
            itemValue = new ItemValue
            {
                ItemValueId = Guid.NewGuid(),
                Type = type,
                Value = value,
                CleanValue = value.ToLowerInvariant()
            };
            context.ItemValues.Add(itemValue);
        }

        context.ItemValuesMap.Add(new ItemValueMap
        {
            ItemId = itemId,
            ItemValueId = itemValue.ItemValueId,
            Item = context.BaseItems.Find(itemId)!,
            ItemValue = itemValue
        });
        context.SaveChanges();
    }

    private void AddLanguages(Guid itemId, string audioLanguage, string subtitleLanguage)
    {
        using var context = CreateDbContext();
        var item = context.BaseItems.Find(itemId)!;
        context.MediaStreamInfos.AddRange(
            new MediaStreamInfo
            {
                ItemId = itemId,
                Item = item,
                StreamIndex = 0,
                StreamType = MediaStreamTypeEntity.Audio,
                Language = audioLanguage
            },
            new MediaStreamInfo
            {
                ItemId = itemId,
                Item = item,
                StreamIndex = 1,
                StreamType = MediaStreamTypeEntity.Subtitle,
                Language = subtitleLanguage
            });
        context.SaveChanges();
    }
}
