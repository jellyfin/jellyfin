using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using LinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers the filters that resolve "folders with a matching descendant" through
/// <see cref="DescendantQueryHelper.GetFolderIdsMatching"/>, both in their positive and their
/// negated form, so the sub-selects they build stay translatable on the SQLite provider.
/// </summary>
public sealed class BaseItemRepositoryStreamFilterTests : IDisposable
{
    private const string FolderType = "MediaBrowser.Controller.Entities.Folder";
    private const string BoxSetType = "MediaBrowser.Controller.Entities.Movies.BoxSet";
    private const string MovieType = "MediaBrowser.Controller.Entities.Movies.Movie";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly BaseItemRepository _repository;

    private readonly Guid _library = Guid.NewGuid();
    private readonly Guid _withSubtitles = Guid.NewGuid();
    private readonly Guid _withoutSubtitles = Guid.NewGuid();
    private readonly Guid _collection = Guid.NewGuid();
    private readonly Guid _linkedSeries = Guid.NewGuid();
    private readonly Guid _linkedEpisode = Guid.NewGuid();

    public BaseItemRepositoryStreamFilterTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var ctx = CreateDbContext())
        {
            ctx.Database.EnsureCreated();
            Seed(ctx);
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        var serverConfigurationManager = new Mock<IServerConfigurationManager>();
        serverConfigurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        _repository = new BaseItemRepository(
            factory.Object,
            new Mock<IServerApplicationHost>().Object,
            new ItemTypeLookup(),
            serverConfigurationManager.Object,
            NullLogger<BaseItemRepository>.Instance);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public void HasSubtitles_MatchesTheItemAndItsParentFolder()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasSubtitles = true });

        Assert.Contains(_withSubtitles, ids);
        // The library is a folder, and it has a descendant with subtitles.
        Assert.Contains(_library, ids);
        Assert.DoesNotContain(_withoutSubtitles, ids);
    }

    [Fact]
    public void HasSubtitles_Negated_ExcludesTheItemAndItsParentFolder()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasSubtitles = false });

        Assert.Contains(_withoutSubtitles, ids);
        Assert.DoesNotContain(_withSubtitles, ids);
        Assert.DoesNotContain(_library, ids);
    }

    [Fact]
    public void SubtitleLanguages_MatchesTheRequestedLanguageOnly()
    {
        Assert.Contains(_withSubtitles, _repository.GetItemIdsList(new InternalItemsQuery { SubtitleLanguages = ["ger"] }));
        Assert.DoesNotContain(_withSubtitles, _repository.GetItemIdsList(new InternalItemsQuery { SubtitleLanguages = ["fre"] }));
    }

    [Fact]
    public void HasNoSubtitleTrackWithLanguage_ExcludesTheMatchingItemAndFolder()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasNoSubtitleTrackWithLanguage = "ger" });

        Assert.Contains(_withoutSubtitles, ids);
        Assert.DoesNotContain(_withSubtitles, ids);
        Assert.DoesNotContain(_library, ids);
    }

    [Fact]
    public void HasSubtitles_MatchesACollectionLinkingAFolder()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasSubtitles = true });

        // The collection links the series, and the subtitles hang off the series' episode.
        Assert.Contains(_linkedSeries, ids);
        Assert.Contains(_collection, ids);
    }

    [Fact]
    public void HasSubtitles_Negated_ExcludesACollectionLinkingAFolder()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasSubtitles = false });

        Assert.DoesNotContain(_linkedSeries, ids);
        Assert.DoesNotContain(_collection, ids);
    }

    [Fact]
    public void HasChapterImages_MatchesTheItemAndItsParentFolder()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasChapterImages = true });

        Assert.Contains(_withSubtitles, ids);
        Assert.Contains(_library, ids);
        Assert.DoesNotContain(_withoutSubtitles, ids);
    }

    private void Seed(JellyfinDbContext context)
    {
        context.BaseItems.Add(new BaseItemEntity { Id = _library, Type = FolderType, Name = "Library", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _withSubtitles, Type = MovieType, Name = "With subtitles" });
        context.BaseItems.Add(new BaseItemEntity { Id = _withoutSubtitles, Type = MovieType, Name = "Without subtitles" });

        foreach (var itemId in new[] { _withSubtitles, _withoutSubtitles })
        {
            context.AncestorIds.Add(new AncestorId
            {
                ItemId = itemId,
                ParentItemId = _library,
                Item = null!,
                ParentItem = null!
            });

            context.MediaStreamInfos.Add(new MediaStreamInfo
            {
                ItemId = itemId,
                StreamIndex = 0,
                StreamType = MediaStreamTypeEntity.Video,
                Item = null!
            });
        }

        context.MediaStreamInfos.Add(new MediaStreamInfo
        {
            ItemId = _withSubtitles,
            StreamIndex = 1,
            StreamType = MediaStreamTypeEntity.Subtitle,
            Language = "ger",
            Item = null!
        });

        // A collection linking a folder: the match is two edges away, one link then one closure hop.
        context.BaseItems.Add(new BaseItemEntity { Id = _collection, Type = BoxSetType, Name = "Collection", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _linkedSeries, Type = FolderType, Name = "Linked series", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _linkedEpisode, Type = MovieType, Name = "Linked episode" });

        context.AncestorIds.Add(new AncestorId
        {
            ItemId = _linkedEpisode,
            ParentItemId = _linkedSeries,
            Item = null!,
            ParentItem = null!
        });

        context.LinkedChildren.Add(new LinkedChildEntity
        {
            ParentId = _collection,
            ChildId = _linkedSeries,
            ChildType = LinkedChildType.Manual,
            SortOrder = 0
        });

        context.MediaStreamInfos.Add(new MediaStreamInfo
        {
            ItemId = _linkedEpisode,
            StreamIndex = 0,
            StreamType = MediaStreamTypeEntity.Subtitle,
            Language = "ger",
            Item = null!
        });

        context.Chapters.Add(new Chapter
        {
            ItemId = _withSubtitles,
            ChapterIndex = 0,
            StartPositionTicks = 0,
            ImagePath = "/chapter.jpg",
            Item = null!
        });

        context.SaveChanges();
    }

    private JellyfinDbContext CreateDbContext()
        => new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
}
