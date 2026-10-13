using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// A filter that looks up an exact value does not remove a NUL character from it, so a value that contains one
/// matches nothing. SQLite compares it as given and returns no rows; an engine that rejects a NUL in a query
/// parameter would raise an error instead.
/// </summary>
public sealed class BaseItemRepositoryExactMatchNulTests : JellyfinDbTestFixture
{
    private const string MoviePath = "/media/movies/bob.mkv";

    private readonly ItemTypeLookup _itemTypeLookup = new();
    private readonly BaseItemRepository _repository;

    public BaseItemRepositoryExactMatchNulTests()
    {
        // The Path filter goes through the host's virtual path mapping before it is compared.
        var appHost = new Mock<IServerApplicationHost>();
        appHost.Setup(h => h.ReverseVirtualPath(It.IsAny<string>())).Returns((string p) => p);

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        _repository = new BaseItemRepository(
            CreateDbContextFactory(),
            appHost.Object,
            _itemTypeLookup,
            configurationManager.Object,
            NullLogger<BaseItemRepository>.Instance);
    }

    [Fact]
    public void GetItemList_PathContainingNul_MatchesNothing()
    {
        AddMovie();
        var user = new User("test", "auth", "reset");

        Assert.Single(_repository.GetItemList(new InternalItemsQuery(user) { Path = MoviePath }));
        KnownGap.On(
            KnownGaps.NulInExactMatchParameter,
            () => Assert.Empty(_repository.GetItemList(new InternalItemsQuery(user) { Path = MoviePath + "\0" })));
    }

    [Fact]
    public void GetItemList_PathContainingNulWithoutGrouping_MatchesNothing()
    {
        AddMovie();
        var user = new User("test", "auth", "reset");

        Assert.Single(_repository.GetItemList(new InternalItemsQuery(user) { Path = MoviePath, GroupByPresentationUniqueKey = false }));
        KnownGap.On(
            KnownGaps.NulInExactMatchParameter,
            () => Assert.Empty(_repository.GetItemList(new InternalItemsQuery(user) { Path = MoviePath + "\0", GroupByPresentationUniqueKey = false })));
    }

    private void AddMovie()
    {
        var id = Guid.NewGuid();
        using var context = CreateDbContext();
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            PresentationUniqueKey = id.ToString("N"),
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
            Name = "Bob",
            Path = MoviePath,
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        });
        context.SaveChanges();
    }
}
