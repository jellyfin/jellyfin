using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Tests.Item;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// Statistics taken on a freshly created database describe every table as a single row, and SQLite then plans
/// the user data and series queries of a filled library as full scans (#17886).
/// </summary>
public sealed class SqliteDatabaseStatisticsTests : SqliteDbTestFixture
{
    private readonly SqliteDatabaseProvider _provider;

    public SqliteDatabaseStatisticsTests()
    {
        _provider = new SqliteDatabaseProvider(ApplicationPaths, NullLogger<SqliteDatabaseProvider>.Instance)
        {
            DbContextFactory = CreateDbContextFactory()
        };
    }

    [Fact]
    public async Task RunScheduledOptimisation_EmptyLibrary_RecordsNoStatistics()
    {
        SeedFolders(3);

        await _provider.RunScheduledOptimisation(CancellationToken.None);

        Assert.Null(ReadAnalyzedItemCount());
    }

    [Fact]
    public async Task RunScheduledOptimisation_LibraryWithItems_RecordsStatistics()
    {
        SeedFolders(1);
        SeedEpisodes(4);

        await _provider.RunScheduledOptimisation(CancellationToken.None);

        Assert.Equal(CountItems(), ReadAnalyzedItemCount());
    }

    [Fact]
    public async Task RefreshStatistics_NoStatistics_Analyzes()
    {
        SeedEpisodes(5);

        await _provider.RefreshStatistics(CancellationToken.None);

        Assert.Equal(CountItems(), ReadAnalyzedItemCount());
    }

    [Fact]
    public async Task RefreshStatistics_LibraryChanged_Reanalyzes()
    {
        SeedEpisodes(10);
        Analyze();
        SeedEpisodes(5);

        await _provider.RefreshStatistics(CancellationToken.None);

        Assert.Equal(CountItems(), ReadAnalyzedItemCount());
    }

    [Fact]
    public async Task RefreshStatistics_EmptyLibrary_RecordsNoStatistics()
    {
        SeedFolders(2);

        await _provider.RefreshStatistics(CancellationToken.None);

        Assert.Null(ReadAnalyzedItemCount());
    }

    private void SeedFolders(int count)
    {
        using var context = CreateDbContext();
        context.BaseItems.AddRange(Enumerable.Range(0, count).Select(_ => new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = "MediaBrowser.Controller.Entities.Folder",
            IsFolder = true
        }));
        context.SaveChanges();
    }

    private void SeedEpisodes(int count)
    {
        using var context = CreateDbContext();
        context.BaseItems.AddRange(Enumerable.Range(0, count).Select(_ => new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = "MediaBrowser.Controller.Entities.TV.Episode",
            IsFolder = false
        }));
        context.SaveChanges();
    }

    private void Analyze()
    {
        using var context = CreateDbContext();
        context.Database.ExecuteSqlRaw("ANALYZE");
    }

    private long CountItems()
    {
        using var context = CreateDbContext();
        return context.BaseItems.LongCount();
    }

    private long? ReadAnalyzedItemCount()
    {
        using var context = CreateDbContext();
        var hasStatistics = context.Database
            .SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM sqlite_schema WHERE type = 'table' AND name = 'sqlite_stat1'")
            .Single();
        if (hasStatistics == 0)
        {
            return null;
        }

        return context.Database
            .SqlQueryRaw<long?>("SELECT max(CAST(stat AS INTEGER)) AS \"Value\" FROM sqlite_stat1 WHERE tbl = 'BaseItems'")
            .Single();
    }
}
