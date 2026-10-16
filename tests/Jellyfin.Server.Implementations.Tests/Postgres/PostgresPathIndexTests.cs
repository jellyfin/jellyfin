using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Implementations.Tests.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using Xunit;
using ItemSortBy = Jellyfin.Data.Enums.ItemSortBy;
using LinkedChildType = MediaBrowser.Controller.Entities.LinkedChildType;
using SortOrder = Jellyfin.Database.Implementations.Enums.SortOrder;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

/// <summary>
/// The path of an item is indexed by a hash index on PostgreSQL, because a btree index entry may hold at most 2704
/// bytes. These tests pin that the index is a hash index, and that every lookup by path the server sends can be
/// answered from it.
/// </summary>
public sealed class PostgresPathIndexTests : JellyfinDbTestFixture
{
    private const string PathIndex = "IX_BaseItems_Path";

    private readonly PathCommandCapture _capture;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IRecordingsManager? _previousRecordingsManager;

    public PostgresPathIndexTests()
        : this(new PathCommandCapture())
    {
    }

    private PostgresPathIndexTests(PathCommandCapture capture)
        : base(capture)
    {
        _capture = capture;

        // BaseItem resolves these through process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;
        _previousRecordingsManager = Video.RecordingsManager;
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>())).Returns([]);
        BaseItem.LibraryManager = libraryManager.Object;
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;
        Video.RecordingsManager = Mock.Of<IRecordingsManager>();
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public void PathIndex_IsAHashIndexInTheModelAndInTheDatabase()
    {
        using var context = CreateDbContext();
        var index = context.Model.FindEntityType(typeof(BaseItemEntity))!.GetIndexes().Single(i => i.GetDatabaseName() == PathIndex);

        // A hash index cannot be unique and holds no nulls: the path is not unique, and no lookup asks for a null path.
        Assert.Equal("hash", index.GetMethod());
        Assert.False(index.IsUnique);
        Assert.Null(index.GetFilter());
        Assert.Null(index.GetNullSortOrder());
        Assert.EndsWith(
            "USING hash (\"Path\")",
            context.Database
                .SqlQueryRaw<string>($"""SELECT indexdef AS "Value" FROM pg_indexes WHERE schemaname = current_schema() AND indexname = '{PathIndex}'""")
                .Single(),
            StringComparison.Ordinal);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public void Indexes_AreBtreeIndexesExceptThePathIndex()
    {
        using var context = CreateDbContext();

        var notBtree = context.Database
            .SqlQueryRaw<string>(
                """
                SELECT c.relname || ' ' || m.amname AS "Value"
                FROM pg_index i
                JOIN pg_class c ON c.oid = i.indexrelid
                JOIN pg_am m ON m.oid = c.relam
                WHERE c.relnamespace = current_schema()::regnamespace
                  AND m.amname <> 'btree'
                """)
            .ToList();

        // Any other index that is not a btree has to be a decision of its own, recorded here.
        Assert.Equal([PathIndex + " hash"], notBtree);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public void LookupsByPath_OfTheServer_AreAnsweredFromThePathIndex()
    {
        var known = new[] { "/movies/a.mkv", "/movies/b.mkv", "/movies/c.mkv" };
        var service = CreateService();
        service.SaveItems(known.Select(p => (BaseItem)new Movie { Id = Guid.NewGuid(), Name = p, Path = p }).ToList(), CancellationToken.None);

        _capture.Enabled = true;

        // An item by its path, as the library looks one up.
        Assert.Single(CreateRepository().GetItemList(new InternalItemsQuery
        {
            Path = known[0],
            IsFolder = false,
            OrderBy = [(ItemSortBy.DateCreated, SortOrder.Descending)],
            Limit = 1,
            DtoOptions = new DtoOptions(true)
        }));

        // The children of a playlist that are known only by their path, and the local alternate versions of a video.
        var playlist = new Playlist { Id = Guid.NewGuid(), Name = "Playlist", Path = "/playlists/p.m3u" };
#pragma warning disable CS0618 // Type or member is obsolete: a child known only by its path is the lookup under test.
        playlist.LinkedChildren = known.Select(p => new LinkedChild { Path = p, Type = LinkedChildType.Manual }).ToArray();
#pragma warning restore CS0618
        var video = new Movie { Id = Guid.NewGuid(), Name = "Video", Path = "/movies/v.mkv", LocalAlternateVersions = known[1..] };
        service.SaveItems([playlist, video], CancellationToken.None);

        _capture.Enabled = false;

        // The commands that look items up by path: one value, and a list of them.
        var lookups = _capture.Commands.Where(c => c.Sql.Contains("\"Path\" = @", StringComparison.Ordinal) || c.Sql.Contains("\"Path\" = ANY (@", StringComparison.Ordinal)).ToList();
        Assert.Contains(lookups, c => c.Sql.Contains("\"Path\" = @", StringComparison.Ordinal));
        Assert.Equal(2, lookups.Count(c => c.Sql.Contains("\"Path\" = ANY (@", StringComparison.Ordinal)));

        using var context = CreateDbContext();
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        connection.Open();

        // An almost empty table is always scanned, so the planner is told that a scan is the worse choice. The plan
        // then shows whether the index can answer the lookup at all.
        using (var disable = new NpgsqlCommand("SET enable_seqscan = off", connection))
        {
            disable.ExecuteNonQuery();
        }

        Assert.All(lookups, lookup =>
        {
#pragma warning disable CA2100 // The statement is one that EF Core generated, with its parameters.
            using var explain = new NpgsqlCommand("EXPLAIN " + lookup.Sql, connection);
#pragma warning restore CA2100
            explain.Parameters.AddRange(lookup.Parameters.Select(p => p.Clone()).ToArray());
            using var reader = explain.ExecuteReader();
            var plan = new List<string>();
            while (reader.Read())
            {
                plan.Add(reader.GetString(0));
            }

            Assert.True(
                plan.Any(line => line.Contains(PathIndex, StringComparison.Ordinal)),
                $"The lookup is not answered from {PathIndex}:{Environment.NewLine}{lookup.Sql}{Environment.NewLine}{string.Join(Environment.NewLine, plan)}");
        });
    }

    protected override void Dispose(bool disposing)
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        Video.RecordingsManager = _previousRecordingsManager!;
        base.Dispose(disposing);
    }

    private static IServerApplicationHost CreateAppHost()
    {
        // Paths round-trip through the host's virtual path mapping on the way in and out.
        var appHost = new Mock<IServerApplicationHost>();
        appHost.Setup(h => h.ReverseVirtualPath(It.IsAny<string>())).Returns((string p) => p);
        appHost.Setup(h => h.ExpandVirtualPath(It.IsAny<string>())).Returns((string p) => p);
        return appHost.Object;
    }

    private ItemPersistenceService CreateService()
        => new(CreateDbContextFactory(), DatabaseProvider, CreateAppHost(), NullLogger<ItemPersistenceService>.Instance);

    private BaseItemRepository CreateRepository()
    {
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        return new BaseItemRepository(CreateDbContextFactory(), CreateAppHost(), new ItemTypeLookup(), configurationManager.Object, NullLogger<BaseItemRepository>.Instance);
    }

    /// <summary>
    /// Keeps the queries that name the path column, with copies of their parameters, while enabled.
    /// </summary>
    private sealed class PathCommandCapture : DbCommandInterceptor
    {
        public bool Enabled { get; set; }

        public List<(string Sql, NpgsqlParameter[] Parameters)> Commands { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            if (Enabled && command.CommandText.Contains("\"Path\"", StringComparison.Ordinal))
            {
                Commands.Add((command.CommandText, command.Parameters.Cast<NpgsqlParameter>().Select(p => p.Clone()).ToArray()));
            }

            return result;
        }
    }
}
