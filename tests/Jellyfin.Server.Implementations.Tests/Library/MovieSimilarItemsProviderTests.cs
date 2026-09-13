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
    private readonly MovieSimilarItemsProvider _provider;
    private readonly User _user = new("test", "auth-provider", "reset-provider");
    private readonly string _movieTypeName;

    private readonly Guid _source = Guid.NewGuid();
    private readonly Guid _sourceAlternate = Guid.NewGuid();
    private readonly Guid _similar = Guid.NewGuid();
    private readonly Guid _similarAlternate = Guid.NewGuid();
    private readonly Guid _unrelated = Guid.NewGuid();

    public MovieSimilarItemsProviderTests()
    {
        var itemTypeLookup = new ItemTypeLookup();
        _movieTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie]!;

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
            new Mock<ILibraryManager>().Object);
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

    private async Task<List<Guid>> GetSimilarItemsAsync()
    {
        var results = await _provider.GetSimilarItemsAsync(
            new Movie { Id = _source, Name = "Source" },
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

        context.Users.Add(_user);
        context.ItemValues.AddRange(shared, other);
        context.ItemValuesMap.AddRange(
            CreateMap(source, shared),
            CreateMap(sourceAlternate, shared),
            CreateMap(similar, shared),
            CreateMap(similarAlternate, shared),
            CreateMap(unrelated, other));

        context.SaveChanges();
    }

    private BaseItemEntity AddMovie(JellyfinDbContext context, Guid id, string name, Guid? primaryVersionId)
    {
        var item = new BaseItemEntity
        {
            Id = id,
            Type = _movieTypeName,
            Name = name,
            SortName = name,
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
