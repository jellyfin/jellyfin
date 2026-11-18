using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Data;
using Emby.Server.Implementations.Library.SimilarItems;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Tests.Item;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Library;

/// <summary>
/// Covers how <see cref="MovieSimilarItemsProvider"/> treats alternate versions: they share their
/// primary's genres, tags, studios and people, so they score like it and must not be offered as
/// something similar - neither as another copy of a recommendation nor as a match for the source.
/// </summary>
public sealed class MovieSimilarItemsProviderTests : SqliteDbTestFixture
{
    private static readonly Guid _movieLibraryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _movie4KLibraryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly MovieSimilarItemsProvider _provider;
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly User _user = new("test", "auth-provider", "reset-provider");
    private readonly string _movieTypeName;
    private readonly string _folderTypeName;

    private readonly Guid _source = Guid.NewGuid();
    private readonly Guid _sourceAlternate = Guid.NewGuid();
    private readonly Guid _similar = Guid.NewGuid();
    private readonly Guid _similarAlternate = Guid.NewGuid();
    private readonly Guid _unrelated = Guid.NewGuid();

    // A second scenario, in two libraries and on a genre of its own, for the group whose primary the
    // user may not be able to reach at all.
    private readonly Guid _crossSource = Guid.NewGuid();
    private readonly Guid _crossLibraryPrimary = Guid.NewGuid();
    private readonly Guid _crossLibraryVersion = Guid.NewGuid();
    private readonly Guid _sameLibraryPrimary = Guid.NewGuid();
    private readonly Guid _sameLibraryVersion = Guid.NewGuid();

    public MovieSimilarItemsProviderTests()
    {
        var itemTypeLookup = new ItemTypeLookup();
        _movieTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie]!;
        _folderTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Folder]!;

        using (var context = CreateDbContext())
        {
            Seed(context);
        }

        var serverConfigurationManager = new Mock<IServerConfigurationManager>();
        serverConfigurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        _provider = new MovieSimilarItemsProvider(
            CreateDbContextFactory(),
            CreateBaseItemRepository(itemTypeLookup),
            serverConfigurationManager.Object,
            _libraryManager.Object);
    }

    [Fact]
    public async Task GetSimilarItems_ReturnsThePrimaryAndNeitherVersionOfTheSource()
    {
        var items = await GetSimilarItemsAsync().ConfigureAwait(true);

        Assert.Equal([_similar], items);
    }

    [Fact]
    public async Task GetSimilarItems_DoesNotOfferAnAlternateVersionOfAMatch()
    {
        var items = await GetSimilarItemsAsync().ConfigureAwait(true);

        Assert.DoesNotContain(_similarAlternate, items);
    }

    [Fact]
    public async Task GetSimilarItems_DoesNotOfferTheSourcesOwnOtherVersion()
    {
        var items = await GetSimilarItemsAsync().ConfigureAwait(true);

        Assert.DoesNotContain(_sourceAlternate, items);
    }

    [Fact]
    public async Task GetSimilarItems_UserWithoutThePrimarysLibrary_OffersTheVersion()
    {
        // The user may only open the library the 1080p version is in, so its primary is out of reach
        // and the version is all that is left to stand in for the group.
        RestrictUserTo(_movieLibraryId);

        var items = await GetSimilarItemsAsync(_crossSource).ConfigureAwait(true);

        Assert.Contains(_crossLibraryVersion, items);
        Assert.DoesNotContain(_crossLibraryPrimary, items);
    }

    [Fact]
    public async Task GetSimilarItems_UserWithBothLibraries_OffersThePrimaryOfTheGroupOnce()
    {
        RestrictUserTo(_movieLibraryId, _movie4KLibraryId);

        var items = await GetSimilarItemsAsync(_crossSource).ConfigureAwait(true);

        Assert.Contains(_crossLibraryPrimary, items);
        Assert.DoesNotContain(_crossLibraryVersion, items);
    }

    [Fact]
    public async Task GetSimilarItems_GroupMergedInsideOneLibrary_StillOffersOnlyThePrimary()
    {
        RestrictUserTo(_movieLibraryId, _movie4KLibraryId);

        var items = await GetSimilarItemsAsync(_crossSource).ConfigureAwait(true);

        Assert.Contains(_sameLibraryPrimary, items);
        Assert.DoesNotContain(_sameLibraryVersion, items);
    }

    private void RestrictUserTo(params Guid[] libraryIds)
    {
        _libraryManager
            .Setup(l => l.ConfigureUserAccess(It.IsAny<InternalItemsQuery>(), It.IsAny<User>()))
            .Callback<InternalItemsQuery, User>((query, _) => query.TopParentIds = libraryIds);
    }

    private async Task<List<Guid>> GetSimilarItemsAsync(Guid? sourceId = null)
    {
        var results = await _provider.GetSimilarItemsAsync(
            new Movie { Id = sourceId ?? _source, Name = "Source" },
            new SimilarItemsQuery { User = _user, Limit = 10, DtoOptions = new DtoOptions() },
            CancellationToken.None).ConfigureAwait(false);

        return results.Select(i => i.Id).ToList();
    }

    private void Seed(JellyfinDbContext context)
    {
        // One shared genre, so every movie but the unrelated one scores against the source.
        var shared = CreateItemValue("Action", "action");
        var other = CreateItemValue("Comedy", "comedy");

        var source = AddMovie(context, _source, "Source", primaryVersionId: null);
        var sourceAlternate = AddMovie(context, _sourceAlternate, "Source 4K", primaryVersionId: _source);
        var similar = AddMovie(context, _similar, "Similar", primaryVersionId: null);
        var similarAlternate = AddMovie(context, _similarAlternate, "Similar 4K", primaryVersionId: _similar);
        var unrelated = AddMovie(context, _unrelated, "Unrelated", primaryVersionId: null);

        // The second scenario scores on a genre of its own, so it stays out of the results above.
        var crossLibrary = CreateItemValue("Science Fiction", "science fiction");

        AddLibrary(context, _movieLibraryId, "Movies");
        AddLibrary(context, _movie4KLibraryId, "Movies-4K");

        var crossSource = AddMovie(context, _crossSource, "Cross Source", primaryVersionId: null, libraryId: _movieLibraryId);

        // The 4K version heads the group and lives in a library of its own.
        var crossLibraryPrimary = AddMovie(context, _crossLibraryPrimary, "Coco 4K", primaryVersionId: null, libraryId: _movie4KLibraryId);
        var crossLibraryVersion = AddMovie(context, _crossLibraryVersion, "Coco", primaryVersionId: _crossLibraryPrimary, libraryId: _movieLibraryId);

        // A group merged inside one library, as a control.
        var sameLibraryPrimary = AddMovie(context, _sameLibraryPrimary, "Up 4K", primaryVersionId: null, libraryId: _movieLibraryId);
        var sameLibraryVersion = AddMovie(context, _sameLibraryVersion, "Up", primaryVersionId: _sameLibraryPrimary, libraryId: _movieLibraryId);

        context.Users.Add(_user);
        context.ItemValues.AddRange(shared, other, crossLibrary);
        context.ItemValuesMap.AddRange(
            CreateMap(source, shared),
            CreateMap(sourceAlternate, shared),
            CreateMap(similar, shared),
            CreateMap(similarAlternate, shared),
            CreateMap(unrelated, other),
            CreateMap(crossSource, crossLibrary),
            CreateMap(crossLibraryPrimary, crossLibrary),
            CreateMap(crossLibraryVersion, crossLibrary),
            CreateMap(sameLibraryPrimary, crossLibrary),
            CreateMap(sameLibraryVersion, crossLibrary));

        context.SaveChanges();
    }

    private void AddLibrary(JellyfinDbContext context, Guid id, string name)
    {
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = _folderTypeName,
            Name = name,
            Path = "/" + name,
            IsFolder = true
        });
    }

    private BaseItemEntity AddMovie(JellyfinDbContext context, Guid id, string name, Guid? primaryVersionId, Guid? libraryId = null)
    {
        var item = new BaseItemEntity
        {
            Id = id,
            Type = _movieTypeName,
            Name = name,
            SortName = name,
            ParentId = libraryId,
            TopParentId = libraryId,
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false,
            // An alternate presents under its primary's key, which is what collapses the group in listings.
            PresentationUniqueKey = (primaryVersionId ?? id).ToString("N"),
            PrimaryVersionId = primaryVersionId
        };

        context.BaseItems.Add(item);
        return item;
    }

    private static ItemValue CreateItemValue(string value, string cleanValue)
        => new()
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Genre,
            Value = value,
            CleanValue = cleanValue
        };

    private static ItemValueMap CreateMap(BaseItemEntity item, ItemValue itemValue)
        => new()
        {
            ItemId = item.Id,
            ItemValueId = itemValue.ItemValueId,
            Item = item,
            ItemValue = itemValue
        };
}
