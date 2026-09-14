using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using LinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class ItemCountServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly IApplicationPaths _applicationPaths;
    private readonly ItemCountService _service;
    private int _contextsCreated;
    private List<string>? _capturedSql;

    public ItemCountServiceTests()
    {
        _applicationPaths = new Mock<IApplicationPaths>().Object;

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .LogTo(CaptureStatement, LogLevel.Information)
            .Options;

        using (var context = CreateDbContext())
        {
            context.Database.EnsureCreated();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(() =>
        {
            _contextsCreated++;
            return CreateDbContext();
        });

        var queryHelpers = new Mock<IItemQueryHelpers>();
        queryHelpers
            .Setup(h => h.ApplyAccessFiltering(
                It.IsAny<JellyfinDbContext>(),
                It.IsAny<IQueryable<BaseItemEntity>>(),
                It.IsAny<InternalItemsQuery>()))
            .Returns((JellyfinDbContext _, IQueryable<BaseItemEntity> query, InternalItemsQuery _) => query);

        var typeLookup = new Mock<IItemTypeLookup>();
        typeLookup.Setup(l => l.BaseItemKindNames).Returns(new Dictionary<BaseItemKind, string>
        {
            [BaseItemKind.Movie] = "Movie",
            [BaseItemKind.Series] = "Series",
            [BaseItemKind.Episode] = "Episode",
            [BaseItemKind.MusicAlbum] = "MusicAlbum",
            [BaseItemKind.MusicArtist] = "MusicArtist",
            [BaseItemKind.MusicVideo] = "MusicVideo",
            [BaseItemKind.Audio] = "Audio",
            [BaseItemKind.Trailer] = "Trailer",
            [BaseItemKind.BoxSet] = "BoxSet",
            [BaseItemKind.Book] = "Book",
            [BaseItemKind.LiveTvProgram] = "LiveTvProgram"
        });

        _service = new ItemCountService(
            factory.Object,
            typeLookup.Object,
            queryHelpers.Object);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private void CaptureStatement(string message)
    {
        if (_capturedSql is not null && message.Contains("SELECT", StringComparison.Ordinal))
        {
            _capturedSql.Add(message[message.IndexOf("SELECT", StringComparison.Ordinal)..]);
        }
    }

    [Fact]
    public void GetChildCountBatch_LargeParentIdSet_DoesNotExceedSqliteVariableLimit()
    {
        var hierarchicalParentId = Guid.NewGuid();
        var linkedParentId = Guid.NewGuid();

        var hierarchicalChildId = Guid.NewGuid();
        var linkedChildId1 = Guid.NewGuid();
        var linkedChildId2 = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            context.BaseItems.AddRange(
                CreateItem(hierarchicalParentId),
                CreateItem(linkedParentId),
                CreateItem(hierarchicalChildId, hierarchicalParentId),
                CreateItem(linkedChildId1),
                CreateItem(linkedChildId2));

            context.LinkedChildren.AddRange(
                new LinkedChildEntity
                {
                    ParentId = linkedParentId,
                    ChildId = linkedChildId1,
                    ChildType = LinkedChildType.Manual,
                    SortOrder = 0
                },
                new LinkedChildEntity
                {
                    ParentId = linkedParentId,
                    ChildId = linkedChildId2,
                    ChildType = LinkedChildType.Manual,
                    SortOrder = 1
                });

            context.SaveChanges();
        }

        var parentIds = Enumerable.Range(0, 40_000)
            .Select(_ => Guid.NewGuid())
            .ToList();

        parentIds.Add(hierarchicalParentId);
        parentIds.Add(linkedParentId);

        var result = _service.GetChildCountBatch(parentIds, null);

        Assert.Equal(1, result[hierarchicalParentId]);
        Assert.Equal(2, result[linkedParentId]);
        Assert.Equal(parentIds.Count, result.Count);
    }

    [Fact]
    public void GetCounts_MergedFolders_CountLeavesOfEveryFolderInTheGroup()
    {
        // Two folder-items of one merged series: same presentation key, a leaf each, one of them played.
        var (user, seriesA, seriesB) = SeedMergedSeries(out var playedLeafId);

        var filter = new InternalItemsQuery(user);

        // Either folder-item stands for the whole merged series, so both must report the group.
        foreach (var seriesId in new[] { seriesA, seriesB })
        {
            Assert.Equal(2, _service.GetTotalCount(filter, seriesId));
            Assert.Equal(1, _service.GetPlayedCount(filter, seriesId));
            Assert.Equal((1, 2), _service.GetPlayedAndTotalCount(filter, seriesId));
        }

        var batch = _service.GetPlayedAndTotalCountBatch([seriesA], user);
        Assert.Equal((1, 2), batch[seriesA]);

        Assert.NotEqual(Guid.Empty, playedLeafId);
    }

    [Fact]
    public void GetCounts_UnmergedFolder_CountsOnlyItsOwnLeaves()
    {
        var (user, _, _) = SeedMergedSeries(out _);

        // A folder with a key of its own must not pick up anything from the merged pair.
        var loneSeriesId = Guid.NewGuid();
        var loneLeafId = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            var lone = CreateItem(loneSeriesId);
            lone.PresentationUniqueKey = "lone-series";
            context.BaseItems.Add(lone);
            context.BaseItems.Add(CreateLeaf(loneLeafId));
            context.SaveChanges();
            AddAncestor(context, loneLeafId, loneSeriesId);
            context.SaveChanges();
        }

        var filter = new InternalItemsQuery(user);

        Assert.Equal(1, _service.GetTotalCount(filter, loneSeriesId));
        Assert.Equal(0, _service.GetPlayedCount(filter, loneSeriesId));
        Assert.Equal((0, 1), _service.GetPlayedAndTotalCount(filter, loneSeriesId));
    }

    [Fact]
    public void GetCounts_PlayedAlternateVersion_CountThePrimaryAsPlayed()
    {
        var user = new User("alt-version-test", "provider", "reset");
        var seriesId = Guid.NewGuid();
        var primaryId = Guid.NewGuid();
        var alternateId = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            context.Users.Add(user);

            var series = CreateItem(seriesId);
            series.PresentationUniqueKey = "alt-version-series";
            context.BaseItems.Add(series);

            context.BaseItems.Add(CreateLeaf(primaryId));
            var alternate = CreateLeaf(alternateId);
            alternate.PrimaryVersionId = primaryId;
            context.BaseItems.Add(alternate);
            context.SaveChanges();

            // Only the primary is counted as a leaf, as ApplyAccessFiltering leaves it in production.
            AddAncestor(context, primaryId, seriesId);

            context.LinkedChildren.Add(new LinkedChildEntity
            {
                ParentId = primaryId,
                ChildId = alternateId,
                ChildType = LinkedChildType.LocalAlternateVersion,
                SortOrder = 0
            });

            // The file that was watched is the alternate, so the primary carries no played row.
            context.UserData.Add(new UserData
            {
                ItemId = alternateId,
                UserId = user.Id,
                CustomDataKey = string.Empty,
                Played = true,
                Item = null,
                User = null
            });

            context.SaveChanges();
        }

        var filter = new InternalItemsQuery(user);

        // The per-item paths have to agree with the batch one, which the DTO uses interchangeably.
        Assert.Equal(1, _service.GetPlayedCount(filter, seriesId));
        Assert.Equal((1, 1), _service.GetPlayedAndTotalCount(filter, seriesId));
        Assert.Equal((1, 1), _service.GetPlayedAndTotalCountBatch([seriesId], user)[seriesId]);
    }

    [Fact]
    public void GetChildCountBatch_MergedFolders_CountsDistinctChildKeys()
    {
        var seriesA = Guid.NewGuid();
        var seriesB = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            foreach (var id in new[] { seriesA, seriesB })
            {
                var series = CreateItem(id);
                series.PresentationUniqueKey = "merged-series";
                context.BaseItems.Add(series);
            }

            // Each folder-item holds a "Season 1"; those two share a key and are one season to the user.
            var sharedSeasonA = CreateItem(Guid.NewGuid(), seriesA);
            sharedSeasonA.PresentationUniqueKey = "merged-series-001";
            var sharedSeasonB = CreateItem(Guid.NewGuid(), seriesB);
            sharedSeasonB.PresentationUniqueKey = "merged-series-001";
            var ownSeason = CreateItem(Guid.NewGuid(), seriesB);
            ownSeason.PresentationUniqueKey = "merged-series-002";

            context.BaseItems.AddRange(sharedSeasonA, sharedSeasonB, ownSeason);
            context.SaveChanges();
        }

        var result = _service.GetChildCountBatch([seriesA, seriesB], null);

        Assert.Equal(2, result[seriesA]);
        Assert.Equal(2, result[seriesB]);
    }

    [Fact]
    public void GetChildCountBatch_FlatSeriesStructure_CountsEpisodesUnderTheirSeason()
    {
        var (seriesId, seasonId) = SeedSeries(flat: true, virtualEpisodes: false);

        var result = _service.GetChildCountBatch([seriesId, seasonId], null);

        Assert.Equal(2, result[seasonId]);

        // The series holds the season, not the episodes: counting those here would double them up.
        Assert.Equal(1, result[seriesId]);
    }

    [Fact]
    public void GetChildCountBatch_SeasonFolderStructure_CountsEachEpisodeOnce()
    {
        var (seriesId, seasonId) = SeedSeries(flat: false, virtualEpisodes: false);

        var result = _service.GetChildCountBatch([seriesId, seasonId], null);

        Assert.Equal(2, result[seasonId]);
        Assert.Equal(1, result[seriesId]);
    }

    [Fact]
    public void GetChildCountBatch_MissingEpisodes_CountedUnlessTheUserHidesThem()
    {
        var (_, seasonId) = SeedSeries(flat: false, virtualEpisodes: true);
        var user = new User("count-test", "provider", "reset");

        user.DisplayMissingEpisodes = true;
        Assert.Equal(2, _service.GetChildCountBatch([seasonId], user)[seasonId]);

        // Nothing this user can open, so nothing to report.
        user.DisplayMissingEpisodes = false;
        Assert.Equal(0, _service.GetChildCountBatch([seasonId], user)[seasonId]);
    }

    [Fact]
    public void GetChildCountBatch_NoUser_CountsMissingEpisodes()
    {
        var (_, seasonId) = SeedSeries(flat: false, virtualEpisodes: true);

        Assert.Equal(2, _service.GetChildCountBatch([seasonId], null)[seasonId]);
    }

    private (Guid SeriesId, Guid SeasonId) SeedSeries(bool flat, bool virtualEpisodes)
    {
        var seriesId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();

        using var context = CreateDbContext();
        context.BaseItems.Add(CreateItem(seriesId));
        context.BaseItems.Add(CreateItem(seasonId, seriesId));

        // Flat: the episodes sit in the series folder, so ParentId points at the series and only
        // SeasonId ties them to the season they belong to.
        for (var i = 0; i < 2; i++)
        {
            var episode = CreateItem(Guid.NewGuid(), flat ? seriesId : seasonId);
            episode.Type = "MediaBrowser.Controller.Entities.TV.Episode";
            episode.IsFolder = false;
            episode.IsVirtualItem = virtualEpisodes;
            episode.SeasonId = seasonId;
            context.BaseItems.Add(episode);
        }

        context.SaveChanges();

        return (seriesId, seasonId);
    }

    private (User User, Guid SeriesA, Guid SeriesB) SeedMergedSeries(out Guid playedLeafId)
    {
        var user = new User("count-test", "provider", "reset");
        var seriesA = Guid.NewGuid();
        var seriesB = Guid.NewGuid();
        var leafA = Guid.NewGuid();
        var leafB = Guid.NewGuid();
        playedLeafId = leafA;

        using (var context = CreateDbContext())
        {
            context.Users.Add(user);

            foreach (var id in new[] { seriesA, seriesB })
            {
                var series = CreateItem(id);
                series.PresentationUniqueKey = "merged-series";
                context.BaseItems.Add(series);
            }

            context.BaseItems.AddRange(CreateLeaf(leafA), CreateLeaf(leafB));
            context.SaveChanges();

            AddAncestor(context, leafA, seriesA);
            AddAncestor(context, leafB, seriesB);

            context.UserData.Add(new UserData
            {
                ItemId = leafA,
                UserId = user.Id,
                CustomDataKey = string.Empty,
                Played = true,
                Item = null,
                User = null
            });

            context.SaveChanges();
        }

        return (user, seriesA, seriesB);
    }

    private static void AddAncestor(JellyfinDbContext context, Guid itemId, Guid parentItemId)
    {
        context.AncestorIds.Add(new AncestorId
        {
            ItemId = itemId,
            ParentItemId = parentItemId,
            Item = null!,
            ParentItem = null!
        });
    }

    private static BaseItemEntity CreateLeaf(Guid id)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = "Episode",
            IsFolder = false,
            IsVirtualItem = false,
            PresentationUniqueKey = id.ToString("N")
        };
    }

    [Fact]
    public void GetItemCountsForNameItems_MatchesCountingEachNameItemOnItsOwn()
    {
        // Three genres tagging a different number of movies each, plus one tagging nothing.
        var genres = SeedGenres();

        var filter = new InternalItemsQuery();
        BaseItemKind[] related = [BaseItemKind.Movie, BaseItemKind.Series];

        var batch = _service.GetItemCountsForNameItems(BaseItemKind.Genre, genres, related, filter);

        // Every requested id is answered, so a caller can index the result without checking.
        Assert.Equal(genres.Count, batch.Count);

        foreach (var genreId in genres)
        {
            var single = _service.GetItemCountsForNameItem(BaseItemKind.Genre, genreId, related, filter);

            Assert.Equal(single.MovieCount, batch[genreId].MovieCount);
            Assert.Equal(single.SeriesCount, batch[genreId].SeriesCount);
            Assert.Equal(single.ItemCount, batch[genreId].ItemCount);
        }

        // And the counts are the seeded ones rather than all zero, which would match trivially.
        Assert.Equal([3, 2, 1, 0], genres.Select(g => batch[g].MovieCount).ToArray());
    }

    [Fact]
    public void GetItemCountsForNameItems_UnknownId_CountsZero()
    {
        var unknown = Guid.NewGuid();

        var batch = _service.GetItemCountsForNameItems(
            BaseItemKind.Genre,
            [unknown],
            [BaseItemKind.Movie],
            new InternalItemsQuery());

        Assert.Equal(0, batch[unknown].ItemCount);
    }

    [Fact]
    public void GetItemCountsForNameItems_ArtistTaggedTwiceOnOneAlbum_CountsTheAlbumOnce()
    {
        // An album whose artist is also its album artist maps to the same artist twice.
        var artistId = SeedArtistWithAlbum();

        var filter = new InternalItemsQuery();
        BaseItemKind[] related = [BaseItemKind.MusicAlbum];

        var batch = _service.GetItemCountsForNameItems(BaseItemKind.MusicArtist, [artistId], related, filter);
        var single = _service.GetItemCountsForNameItem(BaseItemKind.MusicArtist, artistId, related, filter);

        Assert.Equal(1, batch[artistId].AlbumCount);
        Assert.Equal(single.AlbumCount, batch[artistId].AlbumCount);
        Assert.Equal(single.ItemCount, batch[artistId].ItemCount);
    }

    /// <summary>
    /// Seeds one artist and a single album tagged with it as both artist and album artist.
    /// </summary>
    /// <returns>The id of the seeded artist.</returns>
    private Guid SeedArtistWithAlbum()
    {
        const string Name = "artist-0";
        var artistId = Guid.NewGuid();
        var albumId = Guid.NewGuid();

        using var context = CreateDbContext();

        var artist = CreateItem(artistId);
        artist.Type = "MusicArtist";
        artist.Name = Name;
        artist.CleanName = Name;
        context.BaseItems.Add(artist);

        var album = CreateItem(albumId);
        album.Type = "MusicAlbum";
        context.BaseItems.Add(album);
        context.SaveChanges();

        foreach (var type in new[] { ItemValueType.Artist, ItemValueType.AlbumArtist })
        {
            var itemValue = new ItemValue
            {
                ItemValueId = Guid.NewGuid(),
                Type = type,
                Value = Name,
                CleanValue = Name
            };
            context.ItemValues.Add(itemValue);
            context.SaveChanges();

            context.ItemValuesMap.Add(new ItemValueMap
            {
                ItemId = albumId,
                ItemValueId = itemValue.ItemValueId,
                Item = null!,
                ItemValue = null!
            });
        }

        context.SaveChanges();

        return artistId;
    }

    [Fact]
    public void GetItemCountsForNameItems_LargeIdSet_DoesNotExceedSqliteVariableLimit()
    {
        // Seeded rather than random, so the clean names of every one of them reach the second
        // query's IN list and the join behind it, instead of stopping at the empty-name return.
        var seeded = SeedArtists(50, out var taggedArtistId);

        var ids = seeded.Concat(Enumerable.Range(0, 40_000).Select(_ => Guid.NewGuid())).ToList();

        var batch = _service.GetItemCountsForNameItems(
            BaseItemKind.MusicArtist,
            ids,
            [BaseItemKind.MusicAlbum],
            new InternalItemsQuery());

        Assert.Equal(ids.Count, batch.Count);

        // And the grouped query really ran, rather than every id coming back zeroed.
        Assert.Equal(1, batch[taggedArtistId].AlbumCount);
    }

    [Fact]
    public void GetItemCountsForNameItems_QueryShape_DoesNotVaryWithBatchSize()
    {
        // Every id list has to be bound as one parameter rather than one placeholder each: that is
        // what keeps the statement off the SQLite variable ceiling and out of a per-size entry in
        // EF's compiled query cache. Identical SQL for two batch sizes is exactly that property.
        var seeded = SeedArtists(6, out _);

        var small = CaptureSql(() => _service.GetItemCountsForNameItems(
            BaseItemKind.MusicArtist, seeded.Take(2).ToList(), [BaseItemKind.MusicAlbum], new InternalItemsQuery()));

        var large = CaptureSql(() => _service.GetItemCountsForNameItems(
            BaseItemKind.MusicArtist, seeded, [BaseItemKind.MusicAlbum], new InternalItemsQuery()));

        Assert.NotEmpty(small);
        Assert.Equal(small, large);
    }

    private List<string> CaptureSql(Action action)
    {
        _capturedSql = [];
        try
        {
            action();
            return _capturedSql;
        }
        finally
        {
            _capturedSql = null;
        }
    }

    /// <summary>
    /// Seeds the requested number of artists, each with a clean name of its own, one of which is
    /// credited on a single album.
    /// </summary>
    /// <param name="count">The number of artists to seed.</param>
    /// <param name="taggedArtistId">The id of the artist credited on an album.</param>
    /// <returns>The ids of the seeded artists.</returns>
    private List<Guid> SeedArtists(int count, out Guid taggedArtistId)
    {
        var ids = new List<Guid>(count);
        using var context = CreateDbContext();

        ItemValue? taggedValue = null;
        taggedArtistId = Guid.Empty;

        for (var i = 0; i < count; i++)
        {
            var name = "bulk-artist-" + i.ToString(CultureInfo.InvariantCulture);
            var artistId = Guid.NewGuid();
            ids.Add(artistId);

            var artist = CreateItem(artistId);
            artist.Type = "MusicArtist";
            artist.Name = name;
            artist.CleanName = name;
            context.BaseItems.Add(artist);

            if (i == 0)
            {
                taggedArtistId = artistId;
                taggedValue = new ItemValue
                {
                    ItemValueId = Guid.NewGuid(),
                    Type = ItemValueType.Artist,
                    Value = name,
                    CleanValue = name
                };
                context.ItemValues.Add(taggedValue);
            }
        }

        context.SaveChanges();

        var albumId = Guid.NewGuid();
        var album = CreateItem(albumId);
        album.Type = "MusicAlbum";
        context.BaseItems.Add(album);
        context.SaveChanges();

        Tag(context, albumId, taggedValue!.ItemValueId);
        context.SaveChanges();

        return ids;
    }

    [Fact]
    public void GetItemCountsForNameItems_KindWithoutItemValues_FallsBackToTheSingleItemPath()
    {
        // Year is keyed by ProductionYear rather than a cleaned item value, so it cannot be grouped.
        var yearId = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            var year = CreateItem(yearId);
            year.Type = "Year";
            year.Name = "2001";
            year.CleanName = "2001";
            context.BaseItems.Add(year);

            for (var i = 0; i < 2; i++)
            {
                var movie = CreateItem(Guid.NewGuid());
                movie.Type = "Movie";
                movie.IsFolder = false;
                movie.ProductionYear = 2001;
                context.BaseItems.Add(movie);
            }

            context.SaveChanges();
        }

        var filter = new InternalItemsQuery();
        BaseItemKind[] related = [BaseItemKind.Movie];

        var batch = _service.GetItemCountsForNameItems(BaseItemKind.Year, [yearId], related, filter);
        var single = _service.GetItemCountsForNameItem(BaseItemKind.Year, yearId, related, filter);

        Assert.Equal(2, batch[yearId].MovieCount);
        Assert.Equal(single.MovieCount, batch[yearId].MovieCount);
    }

    [Fact]
    public void GetItemCountsForNameItems_PeopleAndYears_AreBatchedToo()
    {
        var (personIds, yearIds) = SeedPeopleAndYears();

        var filter = new InternalItemsQuery();
        BaseItemKind[] related = [BaseItemKind.Movie];

        foreach (var (kind, ids) in new[] { (BaseItemKind.Person, personIds), (BaseItemKind.Year, yearIds) })
        {
            var contextsBefore = _contextsCreated;
            var batch = _service.GetItemCountsForNameItems(kind, ids, related, filter);

            // These two used to be answered one query per id; only the value keyed kinds batched.
            Assert.Equal(1, _contextsCreated - contextsBefore);

            Assert.Equal(ids.Count, batch.Count);
            Assert.Equal(2, batch[ids[0]].MovieCount);
            Assert.Equal(1, batch[ids[1]].MovieCount);

            foreach (var id in ids)
            {
                var single = _service.GetItemCountsForNameItem(kind, id, related, filter);
                Assert.Equal(single.MovieCount, batch[id].MovieCount);
                Assert.Equal(single.ItemCount, batch[id].ItemCount);
            }
        }
    }

    /// <summary>
    /// Seeds two people and two years, the first of each on two movies and the second on one.
    /// </summary>
    /// <returns>The ids of the seeded people and years.</returns>
    private (List<Guid> PersonIds, List<Guid> YearIds) SeedPeopleAndYears()
    {
        var personIds = new List<Guid>();
        var yearIds = new List<Guid>();

        using var context = CreateDbContext();

        for (var i = 0; i < 2; i++)
        {
            var personName = "person-" + i.ToString(CultureInfo.InvariantCulture);
            var personId = Guid.NewGuid();
            personIds.Add(personId);

            var person = CreateItem(personId);
            person.Type = "Person";
            person.Name = personName;
            person.CleanName = personName;
            context.BaseItems.Add(person);

            var people = new People { Id = Guid.NewGuid(), Name = personName };
            context.Peoples.Add(people);

            var year = 2000 + i;
            var yearId = Guid.NewGuid();
            yearIds.Add(yearId);

            var yearItem = CreateItem(yearId);
            yearItem.Type = "Year";
            yearItem.Name = year.ToString(CultureInfo.InvariantCulture);
            yearItem.CleanName = yearItem.Name;
            context.BaseItems.Add(yearItem);
            context.SaveChanges();

            // Two movies for the first of each, one for the second.
            for (var m = 0; m < 2 - i; m++)
            {
                var movieId = Guid.NewGuid();
                var movie = CreateItem(movieId);
                movie.Type = "Movie";
                movie.IsFolder = false;
                movie.ProductionYear = year;
                context.BaseItems.Add(movie);
                context.SaveChanges();

                context.PeopleBaseItemMap.Add(new PeopleBaseItemMap
                {
                    ItemId = movieId,
                    PeopleId = people.Id,
                    Item = null!,
                    People = null!,
                    Role = "Actor",
                    ListOrder = m,
                    SortOrder = m
                });
            }

            context.SaveChanges();
        }

        return (personIds, yearIds);
    }

    [Theory]
    // The set the by-name listing actually asks for: it rolls the episodes of a tagged series up
    // into the genre, which is the case the batch has to reproduce query for query.
    [InlineData(BaseItemKind.Episode, BaseItemKind.Series, BaseItemKind.Movie)]
    // And the same seeded data without the roll-up, which takes the plain grouped path.
    [InlineData(BaseItemKind.Movie, BaseItemKind.Series, BaseItemKind.MusicAlbum)]
    public void GetItemCountsForNameItems_TaggedSeriesAndEpisodes_MatchesCountingEachNameItemOnItsOwn(
        BaseItemKind first,
        BaseItemKind second,
        BaseItemKind third)
    {
        var genres = SeedGenresTaggingSeriesAndEpisodes();

        var filter = new InternalItemsQuery();
        BaseItemKind[] related = [first, second, third];

        var batch = _service.GetItemCountsForNameItems(BaseItemKind.Genre, genres, related, filter);

        Assert.Equal(genres.Count, batch.Count);

        foreach (var genreId in genres)
        {
            var single = _service.GetItemCountsForNameItem(BaseItemKind.Genre, genreId, related, filter);

            Assert.Equal(single.EpisodeCount, batch[genreId].EpisodeCount);
            Assert.Equal(single.SeriesCount, batch[genreId].SeriesCount);
            Assert.Equal(single.MovieCount, batch[genreId].MovieCount);
            Assert.Equal(single.ItemCount, batch[genreId].ItemCount);
        }
    }

    [Fact]
    public void GetItemCountsForNameItems_TaggedSeries_RollsEpisodesUpIntoTheGenre()
    {
        var genres = SeedGenresTaggingSeriesAndEpisodes();

        var contextsBefore = _contextsCreated;

        var batch = _service.GetItemCountsForNameItems(
            BaseItemKind.Genre,
            genres,
            [BaseItemKind.Episode, BaseItemKind.Series, BaseItemKind.Movie],
            new InternalItemsQuery());

        // The whole point of the batch: one context for every genre on the page, not one each.
        // The roll-up used to force this shape back onto the single item path.
        Assert.Equal(1, _contextsCreated - contextsBefore);

        // "rolled": one tagged series of two episodes, one of which carries the genre itself, plus
        // a loose tagged episode of an untagged series. The tagged episode of the tagged series
        // must not be counted twice.
        Assert.Equal(3, batch[genres[0]].EpisodeCount);
        Assert.Equal(1, batch[genres[0]].SeriesCount);

        // "loose": a tagged episode whose series carries no genre at all.
        Assert.Equal(1, batch[genres[1]].EpisodeCount);
        Assert.Equal(0, batch[genres[1]].SeriesCount);

        // "empty": tags nothing.
        Assert.Equal(0, batch[genres[2]].EpisodeCount);
    }

    [Fact]
    public void GetItemCountsForNameItems_EpisodeAndItsSeriesTaggedDifferently_KeepsTheGenresApart()
    {
        var seriesId = Guid.NewGuid();
        var episodeId = Guid.NewGuid();
        var genreIds = new List<Guid>();

        using (var context = CreateDbContext())
        {
            var values = new Dictionary<string, Guid>(StringComparer.Ordinal);
            foreach (var name in new[] { "on-series", "on-episode" })
            {
                var genreId = Guid.NewGuid();
                genreIds.Add(genreId);

                var genre = CreateItem(genreId);
                genre.Type = "Genre";
                genre.Name = name;
                genre.CleanName = name;
                context.BaseItems.Add(genre);

                var itemValue = new ItemValue
                {
                    ItemValueId = Guid.NewGuid(),
                    Type = ItemValueType.Genre,
                    Value = name,
                    CleanValue = name
                };
                context.ItemValues.Add(itemValue);
                values[name] = itemValue.ItemValueId;
            }

            var series = CreateItem(seriesId);
            series.Type = "Series";
            context.BaseItems.Add(series);
            context.BaseItems.Add(CreateEpisode(episodeId, seriesId));
            context.SaveChanges();

            Tag(context, seriesId, values["on-series"]);
            Tag(context, episodeId, values["on-episode"]);
            context.SaveChanges();
        }

        var filter = new InternalItemsQuery();
        BaseItemKind[] related = [BaseItemKind.Episode, BaseItemKind.Series, BaseItemKind.Movie];

        var batch = _service.GetItemCountsForNameItems(BaseItemKind.Genre, genreIds, related, filter);

        // The episode rolls up into the genre on its series.
        Assert.Equal(1, batch[genreIds[0]].EpisodeCount);

        // Its own genre is carried by no series, so the episode stays a direct count there. Keyed
        // on the series id alone the episode would be subtracted here and this would read 0.
        Assert.Equal(1, batch[genreIds[1]].EpisodeCount);
        Assert.Equal(0, batch[genreIds[1]].SeriesCount);

        foreach (var genreId in genreIds)
        {
            var single = _service.GetItemCountsForNameItem(BaseItemKind.Genre, genreId, related, filter);
            Assert.Equal(single.EpisodeCount, batch[genreId].EpisodeCount);
            Assert.Equal(single.ItemCount, batch[genreId].ItemCount);
        }
    }

    [Fact]
    public void GetItemCountsForNameItems_TwoNameItemsSharingACleanName_BothGetTheCounts()
    {
        // Distinct rows cleaning down to one name are what the batch keys on; the unique index
        // permits them, so two genre items can legitimately share a clean name.
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var movieId = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            foreach (var (id, name) in new[] { (firstId, "Sci-Fi"), (secondId, "SCI-FI") })
            {
                var genre = CreateItem(id);
                genre.Type = "Genre";
                genre.Name = name;
                genre.CleanName = "sci-fi";
                context.BaseItems.Add(genre);
            }

            var movie = CreateItem(movieId);
            movie.Type = "Movie";
            movie.IsFolder = false;
            context.BaseItems.Add(movie);
            context.SaveChanges();

            foreach (var name in new[] { "Sci-Fi", "SCI-FI" })
            {
                var itemValue = new ItemValue
                {
                    ItemValueId = Guid.NewGuid(),
                    Type = ItemValueType.Genre,
                    Value = name,
                    CleanValue = "sci-fi"
                };
                context.ItemValues.Add(itemValue);
                context.SaveChanges();
                Tag(context, movieId, itemValue.ItemValueId);
            }

            context.SaveChanges();
        }

        var filter = new InternalItemsQuery();
        BaseItemKind[] related = [BaseItemKind.Movie];

        var batch = _service.GetItemCountsForNameItems(BaseItemKind.Genre, [firstId, secondId], related, filter);

        // One movie, reached through two value rows: counted once for each genre item, not twice.
        Assert.Equal(1, batch[firstId].MovieCount);
        Assert.Equal(1, batch[secondId].MovieCount);

        foreach (var genreId in new[] { firstId, secondId })
        {
            var single = _service.GetItemCountsForNameItem(BaseItemKind.Genre, genreId, related, filter);
            Assert.Equal(single.MovieCount, batch[genreId].MovieCount);
        }
    }

    /// <summary>
    /// Seeds three genres: one tagging a series whose episodes roll up (one of them tagged too)
    /// plus a loose episode, one tagging only an episode of an untagged series, and one tagging
    /// nothing.
    /// </summary>
    /// <returns>The ids of the seeded genres, in that order.</returns>
    private List<Guid> SeedGenresTaggingSeriesAndEpisodes()
    {
        var genreIds = new List<Guid>();

        using var context = CreateDbContext();

        var values = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var name in new[] { "rolled", "loose", "empty" })
        {
            var genreId = Guid.NewGuid();
            genreIds.Add(genreId);

            var genre = CreateItem(genreId);
            genre.Type = "Genre";
            genre.Name = name;
            genre.CleanName = name;
            context.BaseItems.Add(genre);

            var itemValue = new ItemValue
            {
                ItemValueId = Guid.NewGuid(),
                Type = ItemValueType.Genre,
                Value = name,
                CleanValue = name
            };
            context.ItemValues.Add(itemValue);
            values[name] = itemValue.ItemValueId;
        }

        context.SaveChanges();

        // A series tagged "rolled" holding two episodes; the second carries "rolled" itself, so the
        // roll-up and the direct tag both see it.
        var taggedSeriesId = Guid.NewGuid();
        var taggedSeries = CreateItem(taggedSeriesId);
        taggedSeries.Type = "Series";
        context.BaseItems.Add(taggedSeries);

        var episodeOfTaggedSeries = CreateEpisode(Guid.NewGuid(), taggedSeriesId);
        var taggedEpisodeOfTaggedSeries = CreateEpisode(Guid.NewGuid(), taggedSeriesId);
        context.BaseItems.AddRange(episodeOfTaggedSeries, taggedEpisodeOfTaggedSeries);

        // An untagged series whose episode carries a genre on its own.
        var untaggedSeriesId = Guid.NewGuid();
        var untaggedSeries = CreateItem(untaggedSeriesId);
        untaggedSeries.Type = "Series";
        context.BaseItems.Add(untaggedSeries);

        var looseEpisode = CreateEpisode(Guid.NewGuid(), untaggedSeriesId);
        var rolledLooseEpisode = CreateEpisode(Guid.NewGuid(), untaggedSeriesId);
        context.BaseItems.AddRange(looseEpisode, rolledLooseEpisode);

        var movieId = Guid.NewGuid();
        var movie = CreateItem(movieId);
        movie.Type = "Movie";
        movie.IsFolder = false;
        context.BaseItems.Add(movie);

        context.SaveChanges();

        Tag(context, taggedSeriesId, values["rolled"]);
        Tag(context, taggedEpisodeOfTaggedSeries.Id, values["rolled"]);
        Tag(context, rolledLooseEpisode.Id, values["rolled"]);
        Tag(context, looseEpisode.Id, values["loose"]);
        Tag(context, movieId, values["rolled"]);

        context.SaveChanges();

        return genreIds;
    }

    private static void Tag(JellyfinDbContext context, Guid itemId, Guid itemValueId)
    {
        context.ItemValuesMap.Add(new ItemValueMap
        {
            ItemId = itemId,
            ItemValueId = itemValueId,
            Item = null!,
            ItemValue = null!
        });
    }

    private static BaseItemEntity CreateEpisode(Guid id, Guid seriesId)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = "Episode",
            IsFolder = false,
            IsVirtualItem = false,
            ParentId = seriesId,
            SeriesId = seriesId
        };
    }

    /// <summary>
    /// Seeds four genres tagging three, two, one and no movies, in that order.
    /// </summary>
    /// <returns>The ids of the seeded genres.</returns>
    private List<Guid> SeedGenres()
    {
        var genreIds = new List<Guid>();

        using var context = CreateDbContext();

        for (var i = 0; i < 4; i++)
        {
            var name = "genre-" + i.ToString(CultureInfo.InvariantCulture);
            var genreId = Guid.NewGuid();
            genreIds.Add(genreId);

            var genre = CreateItem(genreId);
            genre.Type = "Genre";
            genre.Name = name;
            genre.CleanName = name;
            context.BaseItems.Add(genre);

            var itemValue = new ItemValue
            {
                ItemValueId = Guid.NewGuid(),
                Type = ItemValueType.Genre,
                Value = name,
                CleanValue = name
            };
            context.ItemValues.Add(itemValue);
            context.SaveChanges();

            // 3 movies for the first genre, 2 for the second, 1 for the third, none for the last.
            for (var m = 0; m < 3 - i; m++)
            {
                var movieId = Guid.NewGuid();
                var movie = CreateItem(movieId);
                movie.Type = "Movie";
                movie.IsFolder = false;
                context.BaseItems.Add(movie);
                context.SaveChanges();

                context.ItemValuesMap.Add(new ItemValueMap
                {
                    ItemId = movieId,
                    ItemValueId = itemValue.ItemValueId,
                    Item = null!,
                    ItemValue = null!
                });
            }

            context.SaveChanges();
        }

        return genreIds;
    }

    private static BaseItemEntity CreateItem(Guid id, Guid? parentId = null)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = "Folder",
            ParentId = parentId,
            IsFolder = true
        };
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(
                _applicationPaths,
                NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
