using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Emby.Server.Implementations.Data;
using Emby.Server.Implementations.Library.SimilarItems;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using DbLinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers how item queries load linked children. The links are read for a whole result set in one
/// statement rather than joined onto the item row: a container's membership is unbounded, and joined
/// it multiplies against every other collection the query already joins, so one box set with 500
/// members repeats its row once per member per image per user. That product is what made a
/// collection library unservable in issue 18069.
/// <para>
/// Reading them separately means every path that materializes items has to ask for them, and a path
/// that forgets is silently wrong rather than broken: a collection reads as empty, a video loses the
/// versions merged into it, and saving the item back can drop its links. These tests therefore walk
/// every entry point that returns items, and <see cref="EveryQueryEntryPointIsCovered"/> fails when a
/// new one appears without coverage.
/// </para>
/// </summary>
public sealed class BaseItemRepositoryLinkedChildrenTests : SqliteDbTestFixture
{
    private static readonly Guid _collectionsFolderId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid _boxSetId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid _emptyBoxSetId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");
    private static readonly Guid _playlistId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004");
    private static readonly Guid _primaryVideoId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000005");
    private static readonly Guid _mergedVersionId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000006");
    private static readonly Guid _musicFolderId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000007");
    private static readonly Guid _artistId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000008");
    private static readonly Guid _libraryId = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000a");
    private static readonly Guid _albumId = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000b");
    private static readonly Guid _seriesId = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000c");
    private static readonly Guid _seasonId = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000d");
    private static readonly Guid _episodeId = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000e");

    /// <summary>
    /// The types that turn database rows into items, and so have to load the links themselves.
    /// </summary>
    private static readonly Type[] _materializingTypes =
    [
        typeof(BaseItemRepository),
        typeof(NextUpService),
        typeof(MovieSimilarItemsProvider)
    ];

    /// <summary>
    /// Every method on those types that hands back items, each with a test asserting it loads the
    /// links. A new one has to be added here and covered, or <see cref="EveryQueryEntryPointIsCovered"/>
    /// fails — see that test for why a missing one would otherwise go unnoticed.
    /// </summary>
    private static readonly string[] _coveredEntryPoints =
    [
        // BaseItemRepository — covered in this class.
        nameof(BaseItemRepository.GetAlbumArtists),
        nameof(BaseItemRepository.GetAllArtists),
        nameof(BaseItemRepository.GetArtists),
        nameof(BaseItemRepository.GetGenres),
        nameof(BaseItemRepository.GetItemList),
        nameof(BaseItemRepository.GetItems),
        nameof(BaseItemRepository.GetLatestItemList),
        nameof(BaseItemRepository.GetMusicGenres),
        nameof(BaseItemRepository.GetStudios),
        nameof(BaseItemRepository.RetrieveItem),

        // NextUpService — covered in NextUpServiceTests.
        nameof(NextUpService.GetNextUpEpisodesBatch),

        // MovieSimilarItemsProvider — covered in MovieSimilarItemsProviderTests.
        nameof(MovieSimilarItemsProvider.GetSimilarItemsAsync),
        nameof(MovieSimilarItemsProvider.GetBatchSimilarItemsAsync)
    ];

    private readonly CommandRecorder _recorder;
    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _lookup = new();
    private readonly List<Guid> _memberIds = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseItemRepositoryLinkedChildrenTests"/> class.
    /// </summary>
    public BaseItemRepositoryLinkedChildrenTests()
        : this(new CommandRecorder())
    {
    }

    private BaseItemRepositoryLinkedChildrenTests(CommandRecorder recorder)
        : base(recorder)
    {
        _recorder = recorder;
        _repository = CreateBaseItemRepository(_lookup);
        Seed();
    }

    /// <summary>
    /// The collections grid: a listing that returns box sets brings their membership back with it,
    /// in stored order rather than insertion order.
    /// </summary>
    [Fact]
    public void GetItemList_BoxSets_AttachLinkedChildrenInStoredOrder()
    {
        var trilogy = GetCollections().Single(e => e.Id.Equals(_boxSetId));

        Assert.True(trilogy.LinkedChildrenLoaded);
        Assert.Equal(
            _memberIds.AsEnumerable().Reverse(),
            trilogy.LinkedChildren.Select(e => e.ItemId!.Value));
    }

    /// <summary>
    /// A container with no rows still counts as read. An unread container carries the same empty
    /// array, and only this flag stops the save path from taking that as "delete every link".
    /// </summary>
    [Fact]
    public void GetItemList_BoxSetWithoutMembers_IsStillMarkedLoaded()
    {
        var empty = GetCollections().Single(e => e.Id.Equals(_emptyBoxSetId));

        Assert.Empty(empty.LinkedChildren);
        Assert.True(empty.LinkedChildrenLoaded);
    }

    /// <summary>
    /// Playlists keep their contents in the same table and are read the same way.
    /// </summary>
    [Fact]
    public void GetItemList_Playlist_AttachesLinkedChildren()
    {
        var playlist = _repository.GetItemList(new InternalItemsQuery
        {
            ItemIds = [_playlistId],
            DtoOptions = new DtoOptions()
        }).OfType<Playlist>().Single();

        Assert.True(playlist.LinkedChildrenLoaded);
        Assert.Equal(_memberIds.Count, playlist.LinkedChildren.Length);
    }

    /// <summary>
    /// A merged alternate version exists only as a link, and several callers read an empty
    /// <see cref="Video.LinkedAlternateVersions"/> as "this video has a single media source" — so
    /// forgetting to load them under-reports versions instead of failing.
    /// </summary>
    [Fact]
    public void GetItemList_Video_AttachesItsLinkedAlternateVersions()
    {
        var video = _repository.GetItemList(new InternalItemsQuery
        {
            ItemIds = [_primaryVideoId],
            DtoOptions = new DtoOptions()
        }).OfType<Video>().Single();

        var link = Assert.Single(video.LinkedAlternateVersions);
        Assert.Equal(_mergedVersionId, link.ItemId);
    }

    /// <summary>
    /// A box set's members are not the video's business: only the alternate-version links belong to
    /// <see cref="Video.LinkedAlternateVersions"/>, or saving the video would rewrite them as its own.
    /// </summary>
    [Fact]
    public void GetItemList_Video_DoesNotTakeContainerMembershipAsItsVersions()
    {
        var member = _repository.GetItemList(new InternalItemsQuery
        {
            ItemIds = [_memberIds[0]],
            DtoOptions = new DtoOptions()
        }).OfType<Video>().Single();

        Assert.Empty(member.LinkedAlternateVersions);
    }

    /// <summary>
    /// The paged variant runs a different method from the unpaged one and has to load them too.
    /// </summary>
    [Fact]
    public void GetItems_BoxSets_AttachLinkedChildren()
    {
        var result = _repository.GetItems(new InternalItemsQuery
        {
            ParentId = _collectionsFolderId,
            Limit = 10,
            EnableTotalRecordCount = true,
            DtoOptions = new DtoOptions()
        });

        AssertAllContainersLoaded(result.Items);
        Assert.Contains(result.Items.OfType<BoxSet>(), e => e.LinkedChildren.Length == _memberIds.Count);
    }

    /// <summary>
    /// The random-sort branch re-fetches by id through a separate query and used to be the only place
    /// that split the item load, so it is the easiest one to leave behind.
    /// </summary>
    [Fact]
    public void GetItemList_RandomSort_AttachesLinkedChildren()
    {
        var boxSets = _repository.GetItemList(new InternalItemsQuery
        {
            ParentId = _collectionsFolderId,
            OrderBy = [(ItemSortBy.Random, Jellyfin.Database.Implementations.Enums.SortOrder.Ascending)],
            DtoOptions = new DtoOptions()
        });

        AssertAllContainersLoaded(boxSets);
        Assert.Contains(boxSets.OfType<BoxSet>(), e => e.LinkedChildren.Length == _memberIds.Count);
    }

    /// <summary>
    /// Latest-items builds its results through its own id-then-fetch path.
    /// </summary>
    [Fact]
    public void GetLatestItemList_Movies_AttachesLinkedAlternateVersions()
    {
        var items = _repository.GetLatestItemList(
            new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.Movie],
                DtoOptions = new DtoOptions()
            },
            CollectionType.movies);

        var primary = items.OfType<Video>().SingleOrDefault(e => e.Id.Equals(_primaryVideoId));
        Assert.NotNull(primary);
        Assert.Single(primary.LinkedAlternateVersions);
    }

    /// <summary>
    /// The music branch of latest-items reaches the album through a deferred id sub-select, which is a
    /// second, separate load path from the movie branch.
    /// </summary>
    [Fact]
    public void GetLatestItemList_Music_AttachesAlbumLinks()
    {
        var items = _repository.GetLatestItemList(
            new InternalItemsQuery
            {
                TopParentIds = [_libraryId],
                IncludeItemTypes = [BaseItemKind.Audio],
                DtoOptions = new DtoOptions()
            },
            CollectionType.music);

        var album = Assert.Single(items.OfType<MusicAlbum>());
        Assert.True(album.LinkedChildrenLoaded);
    }

    /// <summary>
    /// The TV branch picks a Season or Series container to stand for the episodes and loads those
    /// entities through a third path of its own.
    /// </summary>
    [Fact]
    public void GetLatestItemList_TvShows_AttachesContainerLinks()
    {
        var items = _repository.GetLatestItemList(
            new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.Episode],
                DtoOptions = new DtoOptions()
            },
            CollectionType.tvshows);

        Assert.NotEmpty(items);
        AssertAllContainersLoaded(items);
    }

    /// <summary>
    /// Serving a collection poster loads the box set by id. With the links joined, a 500-member box
    /// set fanned out to tens of thousands of rows for one image request.
    /// </summary>
    [Fact]
    public void RetrieveItem_BoxSet_AttachesLinksWithoutJoiningThem()
    {
        _recorder.Commands.Clear();
        var boxSet = Assert.IsType<BoxSet>(_repository.RetrieveItem(_boxSetId));

        Assert.True(boxSet.LinkedChildrenLoaded);
        Assert.Equal(_memberIds.Count, boxSet.LinkedChildren.Length);
        Assert.DoesNotContain("LEFT JOIN \"LinkedChildren\"", _recorder.Commands[0].Sql, StringComparison.Ordinal);
    }

    /// <summary>
    /// A by-name artist owns no links, but it is a <see cref="Folder"/>, so it still has to come back
    /// read rather than unread.
    /// </summary>
    [Fact]
    public void GetArtists_Artist_IsMarkedLoaded()
    {
        var artists = _repository.GetAllArtists(new InternalItemsQuery { DtoOptions = new DtoOptions() });

        var artist = Assert.Single(artists.Items).Item;
        Assert.True(Assert.IsType<MusicArtist>(artist).LinkedChildrenLoaded);
    }

    /// <summary>
    /// The whole point of reading them separately: the item query must not join the links.
    /// </summary>
    [Fact]
    public void GetItemList_DoesNotJoinLinkedChildrenOntoTheItemQuery()
    {
        _recorder.Commands.Clear();
        GetCollections();

        var itemQuery = _recorder.Commands[0].Sql;
        Assert.Contains("FROM \"BaseItems\"", itemQuery, StringComparison.Ordinal);
        Assert.DoesNotContain("LEFT JOIN \"LinkedChildren\"", itemQuery, StringComparison.Ordinal);
        Assert.Contains(_recorder.Commands, e => e.Sql.Contains("FROM \"LinkedChildren\"", StringComparison.Ordinal));
    }

    /// <summary>
    /// One statement for the whole page, however many containers it holds — not one per item.
    /// </summary>
    [Fact]
    public void GetItemList_ReadsEveryContainersLinksInASingleStatement()
    {
        _recorder.Commands.Clear();
        GetCollections();

        Assert.Single(_recorder.Commands, e => e.Sql.Contains("FROM \"LinkedChildren\"", StringComparison.Ordinal));
    }

    /// <summary>
    /// A result set that can hold no links costs no query at all.
    /// </summary>
    [Fact]
    public void GetItemList_ResultWithoutContainersOrVideos_ReadsNoLinks()
    {
        _recorder.Commands.Clear();
        _repository.GetItemList(new InternalItemsQuery
        {
            ItemIds = [_musicFolderId],
            IncludeItemTypes = [BaseItemKind.MusicGenre],
            DtoOptions = new DtoOptions()
        });

        Assert.DoesNotContain(_recorder.Commands, e => e.Sql.Contains("FROM \"LinkedChildren\"", StringComparison.Ordinal));
    }

    /// <summary>
    /// The tripwire. Every repository method that hands back items has to be listed in
    /// <see cref="_coveredEntryPoints"/> with a test above asserting it loads the links, because a
    /// path that forgets fails silently — the items simply come back with no links.
    /// </summary>
    [Fact]
    public void EveryQueryEntryPointIsCovered()
    {
        var entryPoints = _materializingTypes
            .SelectMany(e => e.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(e => ReturnsItems(e.ReturnType))
            // The per-entity primitive and the loader itself are the building blocks, not entry points.
            .Where(e => e.Name is not (nameof(BaseItemRepository.DeserializeBaseItem) or nameof(BaseItemRepository.LoadLinkedChildren)))
            .Select(e => e.Name)
            .Distinct()
            .OrderBy(e => e, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(_coveredEntryPoints.OrderBy(e => e, StringComparer.Ordinal).ToArray(), entryPoints);
    }

    private static bool ReturnsItems(Type returnType) => ReturnsItems(returnType, []);

    private static bool ReturnsItems(Type type, HashSet<Type> seen)
    {
        if (typeof(BaseItem).IsAssignableFrom(type))
        {
            return true;
        }

        if (!seen.Add(type))
        {
            return false;
        }

        // Unwraps Task<...>, QueryResult<...>, IReadOnlyList<...>, Dictionary<..., ...> and tuples alike.
        if (type.IsGenericType && type.GetGenericArguments().Any(e => ReturnsItems(e, seen)))
        {
            return true;
        }

        // A result object that carries items in its properties counts too, or a method handing back
        // something like NextUpEpisodeBatchResult would look like it returns no items at all.
        return type.Assembly.GetName().Name?.StartsWith("MediaBrowser", StringComparison.Ordinal) == true
            && type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(e => ReturnsItems(e.PropertyType, seen));
    }

    private static void AssertAllContainersLoaded(IEnumerable<BaseItem> items)
    {
        foreach (var folder in items.OfType<Folder>())
        {
            Assert.True(folder.LinkedChildrenLoaded, folder.Name + " came back with its links unread");
        }
    }

    private IReadOnlyList<BoxSet> GetCollections()
        => _repository.GetItemList(new InternalItemsQuery
        {
            ParentId = _collectionsFolderId,
            DtoOptions = new DtoOptions()
        }).OfType<BoxSet>().ToArray();

    private void Seed()
    {
        using var context = CreateDbContext();
        context.BaseItems.Add(CreateItem(_collectionsFolderId, BaseItemKind.Folder, "Collections", "/collections", isFolder: true));
        context.BaseItems.Add(CreateItem(_musicFolderId, BaseItemKind.MusicGenre, "Rock", "/genres/rock"));
        var artist = CreateItem(_artistId, BaseItemKind.MusicArtist, "Artist", "/artists/artist", isFolder: true);
        artist.CleanName = "artist";
        context.BaseItems.Add(artist);

        // A by-name artist is only reachable through a track that credits it.
        var songId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000009");
        var song = CreateItem(songId, BaseItemKind.Audio, "Song", "/music/song.flac");
        song.CleanName = "song";
        song.MediaType = MediaType.Audio.ToString();
        var artistValueId = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
        var artistValue = new ItemValue
        {
            ItemValueId = artistValueId,
            Type = ItemValueType.Artist,
            Value = "Artist",
            CleanValue = "artist"
        };
        context.BaseItems.Add(song);
        context.ItemValues.Add(artistValue);
        context.ItemValuesMap.Add(new ItemValueMap
        {
            ItemId = songId,
            ItemValueId = artistValueId,
            Item = song,
            ItemValue = artistValue
        });

        foreach (var (id, name) in new[] { (_boxSetId, "Trilogy"), (_emptyBoxSetId, "Empty") })
        {
            var boxSet = CreateItem(id, BaseItemKind.BoxSet, name, "/collections/" + name, isFolder: true);
            boxSet.ParentId = _collectionsFolderId;
            context.BaseItems.Add(boxSet);
        }

        context.BaseItems.Add(CreateItem(_playlistId, BaseItemKind.Playlist, "Mix", "/playlists/mix", isFolder: true));

        for (var i = 0; i < 3; i++)
        {
            var suffix = i.ToString(CultureInfo.InvariantCulture);
            var memberId = Guid.Parse("bbbbbbbb-0000-0000-0000-00000000000" + suffix);
            _memberIds.Add(memberId);
            context.BaseItems.Add(CreateItem(memberId, BaseItemKind.Movie, "Member " + suffix, "/movies/m" + suffix));
        }

        // Latest-items has a branch per collection type, each with its own load path.
        var album = CreateItem(_albumId, BaseItemKind.MusicAlbum, "Album", "/music/album", isFolder: true);
        album.TopParentId = _libraryId;
        context.BaseItems.Add(album);
        var track = CreateItem(Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000f"), BaseItemKind.Audio, "Track", "/music/album/track.flac");
        track.TopParentId = _libraryId;
        track.ParentId = _albumId;
        track.MediaType = MediaType.Audio.ToString();
        context.BaseItems.Add(track);

        context.BaseItems.Add(CreateItem(_seriesId, BaseItemKind.Series, "Show", "/tv/show", isFolder: true));
        var season = CreateItem(_seasonId, BaseItemKind.Season, "Season 1", "/tv/show/s1", isFolder: true);
        season.SeriesId = _seriesId;
        season.SeriesName = "Show";
        context.BaseItems.Add(season);
        var episode = CreateItem(_episodeId, BaseItemKind.Episode, "Episode 1", "/tv/show/s1/e1.mkv");
        episode.SeriesId = _seriesId;
        episode.SeriesName = "Show";
        episode.SeasonId = _seasonId;
        episode.MediaType = MediaType.Video.ToString();
        context.BaseItems.Add(episode);

        context.BaseItems.Add(CreateItem(_primaryVideoId, BaseItemKind.Movie, "Feature", "/movies/feature-4k.mkv"));
        context.BaseItems.Add(CreateItem(_mergedVersionId, BaseItemKind.Movie, "Feature", "/movies/feature-1080p.mkv"));
        context.SaveChanges();

        // Members are linked in reverse so a test can tell stored order from insertion order.
        for (var i = 0; i < _memberIds.Count; i++)
        {
            var childId = _memberIds[_memberIds.Count - 1 - i];
            context.LinkedChildren.Add(new LinkedChildEntity
            {
                ParentId = _boxSetId,
                ChildId = childId,
                ChildType = DbLinkedChildType.Manual,
                SortOrder = i
            });
            context.LinkedChildren.Add(new LinkedChildEntity
            {
                ParentId = _playlistId,
                ChildId = childId,
                ChildType = DbLinkedChildType.Manual,
                SortOrder = i
            });
        }

        context.LinkedChildren.Add(new LinkedChildEntity
        {
            ParentId = _primaryVideoId,
            ChildId = _mergedVersionId,
            ChildType = DbLinkedChildType.LinkedAlternateVersion,
            SortOrder = 0
        });
        context.SaveChanges();
    }

    private BaseItemEntity CreateItem(Guid id, BaseItemKind kind, string name, string path, bool isFolder = false)
        => new()
        {
            Id = id,
            Type = _lookup.BaseItemKindNames[kind]!,
            Name = name,
            SortName = name,
            Path = path,
            IsFolder = isFolder,
            MediaType = kind == BaseItemKind.Movie ? MediaType.Video.ToString() : null,

            // Latest-items dedupes on this and skips rows without one.
            PresentationUniqueKey = id.ToString("N", CultureInfo.InvariantCulture),
            DateCreated = DateTime.UtcNow
        };

    private sealed record RecordedCommand(string Sql);

    private sealed class CommandRecorder : DbCommandInterceptor
    {
        public List<RecordedCommand> Commands { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Commands.Add(new RecordedCommand(command.CommandText));
            return result;
        }
    }
}
