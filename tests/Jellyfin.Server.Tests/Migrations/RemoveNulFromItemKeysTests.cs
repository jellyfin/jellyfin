using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// The values the migration cleans were stored by SQLite, which accepts NUL characters. PostgreSQL rejects them, so
/// no such row can exist there and the tests that seed one only run on SQLite.
/// </summary>
public sealed class RemoveNulFromItemKeysTests : DatabaseTestFixture
{
    private readonly Guid _legacyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _cleanId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly Guid _userId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private readonly CapturingLogger<RemoveNulFromItemKeys> _log = new();
    private readonly WriteRecorder _recorder;

    public RemoveNulFromItemKeysTests()
        : this(new WriteRecorder())
    {
    }

    private RemoveNulFromItemKeysTests(WriteRecorder recorder)
        : base(recorder)
    {
        _recorder = recorder;
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task Perform_ValuesWithNul_RemovesNulFromEveryMatchedColumn()
    {
        await SeedAsync();

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var item = ctx.BaseItems.Single(e => e.Id.Equals(_legacyId));
        Assert.Equal("tvdb-12345", item.PresentationUniqueKey);
        Assert.Equal("tvdb-12345", item.SeriesPresentationUniqueKey);
        Assert.Equal("ext-1", item.ExternalId);
        Assert.Equal("ext-2", item.ExternalSeriesId);
        Assert.Equal("Breaking Bad", item.SeriesName);
        Assert.Equal("12345", ctx.BaseItemProviders.Single(e => e.ItemId.Equals(_legacyId) && e.ProviderId == "Tvdb").ProviderValue);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task Perform_ValuesWithNul_LeavesOtherColumnsAndRowsAlone()
    {
        await SeedAsync();

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var legacy = ctx.BaseItems.Single(e => e.Id.Equals(_legacyId));
        Assert.Equal("Season 1", legacy.Name);
        var clean = ctx.BaseItems.Single(e => e.Id.Equals(_cleanId));
        Assert.Equal("tvdb-999", clean.PresentationUniqueKey);
        Assert.Equal("999", ctx.BaseItemProviders.Single(e => e.ItemId.Equals(_cleanId)).ProviderValue);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task Perform_ProviderIdHoldingOnlyNul_RemovesTheProviderId()
    {
        await SeedAsync();
        using (var ctx = CreateDbContext())
        {
            var legacy = ctx.BaseItems.Single(e => e.Id.Equals(_legacyId));
            ctx.BaseItemProviders.Add(new BaseItemProvider { Item = legacy, ItemId = _legacyId, ProviderId = "Imdb", ProviderValue = "placeholder" });
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
            await ctx.BaseItemProviders
                .Where(e => e.ItemId.Equals(_legacyId) && e.ProviderId == "Imdb")
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.ProviderValue, "\0\0"), TestContext.Current.CancellationToken);
        }

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using (var ctx = CreateDbContext())
        {
            Assert.Equal(["Tvdb"], ctx.BaseItemProviders.Where(e => e.ItemId.Equals(_legacyId)).Select(e => e.ProviderId).ToList());
        }
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task Perform_RunTwice_SecondRunWritesNothing()
    {
        await SeedAsync();
        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);
        var changesBefore = _recorder.WriteCount;

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        Assert.Equal(changesBefore, _recorder.WriteCount);
        using var ctx = CreateDbContext();
        Assert.Equal("tvdb-12345", ctx.BaseItems.Single(e => e.Id.Equals(_legacyId)).PresentationUniqueKey);
    }

    [Fact]
    public async Task Perform_NoNul_WritesNothing()
    {
        using (var ctx = CreateDbContext())
        {
            var clean = new BaseItemEntity { Id = _cleanId, Type = typeof(Series).FullName!, PresentationUniqueKey = "tvdb-999", SeriesName = "Show" };
            ctx.BaseItems.Add(clean);
            ctx.BaseItemProviders.Add(new BaseItemProvider { Item = clean, ItemId = _cleanId, ProviderId = "Tvdb", ProviderValue = "999" });
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var changesBefore = _recorder.WriteCount;

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        Assert.Equal(changesBefore, _recorder.WriteCount);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task Perform_UserDataKeyWithNul_RemovesNulFromTheKey()
    {
        await SeedAsync();
        await AddUserDataAsync("tt0001\0", lastPlayed: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), playCount: 3);
        await AddUserDataAsync("tt0002", lastPlayed: null, playCount: 1);

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var rows = ctx.UserData.AsNoTracking().OrderBy(e => e.CustomDataKey).ToList();
        Assert.Equal(["tt0001", "tt0002"], rows.Select(e => e.CustomDataKey));
        Assert.Equal(3, rows[0].PlayCount);
        Assert.DoesNotContain(_log.Messages, m => m.Contains("Merged", StringComparison.Ordinal));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task Perform_CleanedUserDataKeyCollides_KeepsTheNewerRowAndLogsTheMerge()
    {
        await SeedAsync();
        await AddUserDataAsync("tt0001", lastPlayed: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), playCount: 1);
        await AddUserDataAsync("tt0001\0", lastPlayed: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), playCount: 7);

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var row = Assert.Single(ctx.UserData.AsNoTracking().ToList());
        Assert.Equal("tt0001", row.CustomDataKey);
        Assert.Equal(7, row.PlayCount);
        var summary = Assert.Single(_log.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Merged", StringComparison.Ordinal));
        Assert.Contains("1 user data rows", summary.Message, StringComparison.Ordinal);
        var merge = Assert.Single(_log.Entries, e => e.Level == LogLevel.Debug);
        Assert.Contains(_legacyId.ToString(), merge.Message, StringComparison.Ordinal);
        Assert.Contains(_userId.ToString(), merge.Message, StringComparison.Ordinal);
        Assert.Contains("stored with NUL characters", merge.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(_log.Messages, m => m.Contains("tt0001", StringComparison.Ordinal));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task Perform_CleanedUserDataKeyCollidesWithNewerCleanRow_KeepsTheCleanRow()
    {
        await SeedAsync();
        await AddUserDataAsync("tt0001", lastPlayed: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), playCount: 2);
        await AddUserDataAsync("tt0001\0", lastPlayed: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), playCount: 9);

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var row = Assert.Single(ctx.UserData.AsNoTracking().ToList());
        Assert.Equal("tt0001", row.CustomDataKey);
        Assert.Equal(2, row.PlayCount);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task Perform_CollidingUserDataWithEqualDates_KeepsTheRowWithMoreInformation()
    {
        await SeedAsync();
        var date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await AddUserDataAsync("tt0001", lastPlayed: date, playCount: 1, played: false);
        await AddUserDataAsync("tt0001\0", lastPlayed: date, playCount: 1, played: true);
        await AddUserDataAsync("tt0001\0\0", lastPlayed: date, playCount: 4, played: true);

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var row = Assert.Single(ctx.UserData.AsNoTracking().ToList());
        Assert.Equal("tt0001", row.CustomDataKey);
        Assert.Equal(4, row.PlayCount);
        Assert.True(row.Played);
        Assert.Equal(2, _log.Entries.Count(e => e.Level == LogLevel.Debug));
        Assert.Contains(_log.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("2 user data rows", StringComparison.Ordinal));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task Perform_CollidingUserDataWithEqualDates_PlayedRowWinsOverMorePlays()
    {
        await SeedAsync();
        var date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await AddUserDataAsync("tt0001", lastPlayed: date, playCount: 5, played: false);
        await AddUserDataAsync("tt0001\0", lastPlayed: date, playCount: 1, played: true);

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var row = Assert.Single(ctx.UserData.AsNoTracking().ToList());
        Assert.Equal("tt0001", row.CustomDataKey);
        Assert.True(row.Played);
        Assert.Equal(1, row.PlayCount);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task Perform_CollidingUserDataEqualInEveryField_TheRowWithTheCleanKeyWins()
    {
        await SeedAsync();
        var date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await AddUserDataAsync("tt0001", lastPlayed: date, playCount: 2, played: true, rating: 9);
        await AddUserDataAsync("tt\u00000001", lastPlayed: date, playCount: 2, played: true, rating: 1);

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var row = Assert.Single(ctx.UserData.AsNoTracking().ToList());
        Assert.Equal("tt0001", row.CustomDataKey);
        Assert.Equal(9, row.Rating);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task Perform_UserDataKeyWithNul_SecondRunWritesNothing()
    {
        await SeedAsync();
        await AddUserDataAsync("tt0001", lastPlayed: null, playCount: 1);
        await AddUserDataAsync("tt0001\0", lastPlayed: null, playCount: 2);
        await AddUserDataAsync("tt0002\0", lastPlayed: null, playCount: 1);
        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);
        var changesBefore = _recorder.WriteCount;

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        Assert.Equal(changesBefore, _recorder.WriteCount);
    }

    [Fact]
    public async Task Perform_UserDataWithoutNul_WritesNothing()
    {
        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(new BaseItemEntity { Id = _cleanId, Type = typeof(Series).FullName! });
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await AddUserDataAsync("tt0001", lastPlayed: null, playCount: 1, itemId: _cleanId);
        var changesBefore = _recorder.WriteCount;

        await CreateMigration().PerformAsync(TestContext.Current.CancellationToken);

        Assert.Equal(changesBefore, _recorder.WriteCount);
    }

    private async Task AddUserDataAsync(string key, DateTime? lastPlayed, int playCount, bool played = false, double? rating = null, Guid? itemId = null)
    {
        using var ctx = CreateDbContext();
        if (!ctx.Users.Any(e => e.Id.Equals(_userId)))
        {
            ctx.Users.Add(new User("test", "auth", "reset") { Id = _userId });
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        ctx.UserData.Add(new UserData
        {
            ItemId = itemId ?? _legacyId,
            Item = null,
            UserId = _userId,
            User = null,
            CustomDataKey = "placeholder",
            LastPlayedDate = lastPlayed,
            PlayCount = playCount,
            Played = played,
            Rating = rating
        });
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Written around the context the way code that did not remove NUL stored the key.
        await ctx.UserData
            .Where(e => e.ItemId.Equals(itemId ?? _legacyId) && e.UserId.Equals(_userId) && e.CustomDataKey == "placeholder")
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.CustomDataKey, key), TestContext.Current.CancellationToken);
    }

    private async Task SeedAsync()
    {
        using var ctx = CreateDbContext();
        var legacy = new BaseItemEntity { Id = _legacyId, Type = typeof(Season).FullName!, Name = "Season 1" };
        var clean = new BaseItemEntity { Id = _cleanId, Type = typeof(Series).FullName!, PresentationUniqueKey = "tvdb-999" };
        ctx.BaseItems.AddRange(legacy, clean);
        ctx.BaseItemProviders.Add(new BaseItemProvider { Item = legacy, ItemId = _legacyId, ProviderId = "Tvdb", ProviderValue = "12345" });
        ctx.BaseItemProviders.Add(new BaseItemProvider { Item = clean, ItemId = _cleanId, ProviderId = "Tvdb", ProviderValue = "999" });
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Written around the context the way code that did not remove NUL stored the values.
        await ctx.BaseItems
            .Where(e => e.Id.Equals(_legacyId))
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(e => e.PresentationUniqueKey, "tvdb-12\0345")
                    .SetProperty(e => e.SeriesPresentationUniqueKey, "tvdb-12\0345")
                    .SetProperty(e => e.ExternalId, "ext\0-1")
                    .SetProperty(e => e.ExternalSeriesId, "ext-\02")
                    .SetProperty(e => e.SeriesName, "Breaking\0 Bad"),
                TestContext.Current.CancellationToken);
        await ctx.BaseItemProviders
            .Where(e => e.ItemId.Equals(_legacyId) && e.ProviderId == "Tvdb")
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.ProviderValue, "12\0345"), TestContext.Current.CancellationToken);

        Assert.Contains("\0", ctx.BaseItems.AsNoTracking().Single(e => e.Id.Equals(_legacyId)).PresentationUniqueKey!, StringComparison.Ordinal);
    }

    private RemoveNulFromItemKeys CreateMigration()
    {
        return new RemoveNulFromItemKeys(
            new StartupLogger<RemoveNulFromItemKeys>(_log),
            CreateDbContextFactory());
    }

    private sealed class WriteRecorder : DbCommandInterceptor
    {
        public int WriteCount { get; private set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Record(command);
            return result;
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            Record(command);
            return result;
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            Record(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Record(command);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Record(command);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            Record(command);
            return ValueTask.FromResult(result);
        }

        private void Record(DbCommand command)
        {
            var text = command.CommandText.AsSpan().TrimStart();
            if (text.StartsWith("INSERT", StringComparison.Ordinal)
                || text.StartsWith("UPDATE", StringComparison.Ordinal)
                || text.StartsWith("DELETE", StringComparison.Ordinal))
            {
                WriteCount++;
            }
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IEnumerable<string> Messages => Entries.Select(e => e.Message);

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}
