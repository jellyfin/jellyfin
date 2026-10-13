using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Databases from before the columns with the normalized original title and the lower case name existed hold nothing
/// in them, and a restored backup of such a database may as well. The routine fills them in without loading a table, a
/// batch at a time, and leaves rows alone that already hold the right value.
/// </summary>
public sealed class BackfillNormalisedMatchColumnsTests : DatabaseTestFixture
{
    private readonly CapturingLogger<BackfillNormalisedMatchColumns> _log = new();
    private readonly CommandLog _commands;
    private readonly TransactionLog _transactions;

    public BackfillNormalisedMatchColumnsTests()
        : this(new CommandLog(), new TransactionLog())
    {
    }

    private BackfillNormalisedMatchColumnsTests(CommandLog commands, TransactionLog transactions)
        : base(commands, transactions)
    {
        _commands = commands;
        _transactions = transactions;
    }

    [Fact]
    public async Task Perform_RowsWithoutTheColumns_FillsThem()
    {
        var withTitle = await AddItemAsync("Émile Zola", null);
        var withEmpty = await AddItemAsync("AC\\DC", string.Empty);
        var withoutTitle = await AddItemAsync(null, null);
        var punctuation = await AddItemAsync("---", null);
        var emile = await AddPersonAsync("Émile Zola", string.Empty);
        var bob = await AddPersonAsync("BOB", string.Empty);

        await CreateRoutine().PerformAsync(TestContext.Current.CancellationToken);

        Assert.Equal("emile zola", await ReadItemAsync(withTitle));
        Assert.Equal("ac dc", await ReadItemAsync(withEmpty));
        Assert.Null(await ReadItemAsync(withoutTitle));
        Assert.Equal(string.Empty, await ReadItemAsync(punctuation));
        Assert.Equal("émile zola", await ReadPersonAsync(emile));
        Assert.Equal("bob", await ReadPersonAsync(bob));
    }

    [Fact]
    public async Task Perform_RowsWithAWrongValue_ReplacesIt()
    {
        var stale = await AddItemAsync("Émile Zola", "something else");
        var orphaned = await AddItemAsync(null, "left over");
        var person = await AddPersonAsync("Émile", "EMILE");

        await CreateRoutine().PerformAsync(TestContext.Current.CancellationToken);

        Assert.Equal("emile zola", await ReadItemAsync(stale));
        Assert.Null(await ReadItemAsync(orphaned));
        Assert.Equal("émile", await ReadPersonAsync(person));
    }

    [Fact]
    public async Task Perform_RowsThatAlreadyHoldTheValue_AreNotWritten()
    {
        await AddItemAsync("Émile Zola", "emile zola");
        await AddItemAsync(null, null);
        await AddPersonAsync("Émile", "émile");
        _commands.Updates.Clear();
        _transactions.Commits = 0;

        await CreateRoutine().PerformAsync(TestContext.Current.CancellationToken);

        Assert.Empty(_commands.Updates);
        Assert.Equal(0, _transactions.Commits);
    }

    [Fact]
    public async Task Perform_SecondRun_WritesNothing()
    {
        await AddItemAsync("Émile Zola", null);
        await AddItemAsync("---", null);
        await AddPersonAsync("Émile", string.Empty);
        await AddPersonAsync(string.Empty, string.Empty);
        await CreateRoutine().PerformAsync(TestContext.Current.CancellationToken);
        var updates = _commands.Updates.Count;

        await CreateRoutine().PerformAsync(TestContext.Current.CancellationToken);

        Assert.Equal(updates, _commands.Updates.Count);
    }

    [Fact]
    public async Task Perform_ComputedValueIsEmpty_TerminatesAndStoresIt()
    {
        var punctuation = await AddItemAsync("---", null);
        var nameless = await AddPersonAsync(string.Empty, string.Empty);
        var spaces = await AddPersonAsync("   ", "stale");

        await CreateRoutine(batchSize: 1).PerformAsync(TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, await ReadItemAsync(punctuation));
        Assert.Equal(string.Empty, await ReadPersonAsync(nameless));
        Assert.Equal("   ", await ReadPersonAsync(spaces));
    }

    [Fact]
    public async Task Perform_MoreRowsThanOneBatch_FillsEveryRow()
    {
        var items = new List<Guid>();
        var people = new List<(Guid Id, string Name)>();
        for (var i = 0; i < 23; i++)
        {
            items.Add(await AddItemAsync("Titre É" + i, null));
            var name = "Émile " + i;
            people.Add((await AddPersonAsync(name, string.Empty), name));
        }

        await CreateRoutine(batchSize: 5).PerformAsync(TestContext.Current.CancellationToken);

        using var ctx = CreateDbContext();
        var storedItems = await ctx.BaseItems.AsNoTracking().ToDictionaryAsync(e => e.Id, e => e.CleanOriginalTitle, TestContext.Current.CancellationToken);
        for (var i = 0; i < items.Count; i++)
        {
            Assert.Equal("titre e" + i, storedItems[items[i]]);
        }

        var storedPeople = await ctx.Peoples.AsNoTracking().ToDictionaryAsync(e => e.Id, e => e.NameLower, TestContext.Current.CancellationToken);
        Assert.All(people, e => Assert.Equal(e.Name.ToLowerInvariant(), storedPeople[e.Id]));
    }

    [Fact]
    public async Task Perform_EveryBatchWithChanges_IsCommittedOnItsOwn()
    {
        for (var i = 0; i < 12; i++)
        {
            await AddItemAsync("Title " + i, null);
        }

        _transactions.Commits = 0;

        await CreateRoutine(batchSize: 5).PerformAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, _transactions.Commits);
    }

    [Fact]
    public async Task Perform_BatchFails_RollsBackThatBatchAndKeepsTheBatchesBefore()
    {
        for (var i = 0; i < 12; i++)
        {
            await AddItemAsync("Title " + i, null);
        }

        _commands.FailOnUpdateNumber = _commands.Updates.Count + 8;

        await Assert.ThrowsAnyAsync<Exception>(() => CreateRoutine(batchSize: 5).PerformAsync(TestContext.Current.CancellationToken));

        using var ctx = CreateDbContext();
        var filled = await ctx.BaseItems.AsNoTracking().CountAsync(e => e.CleanOriginalTitle != null, TestContext.Current.CancellationToken);
        Assert.Equal(5, filled);

        _commands.FailOnUpdateNumber = null;
        await CreateRoutine(batchSize: 5).PerformAsync(TestContext.Current.CancellationToken);
        Assert.Equal(12, await ctx.BaseItems.AsNoTracking().CountAsync(e => e.CleanOriginalTitle != null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Perform_Cancelled_StopsBetweenBatchesAndTheNextRunFinishes()
    {
        for (var i = 0; i < 40; i++)
        {
            await AddItemAsync("Title " + i, null);
        }

        using var cancellation = new CancellationTokenSource();
        _log.OnMessage = (level, _) =>
        {
            if (level == LogLevel.Information)
            {
                cancellation.Cancel();
            }
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateRoutine(batchSize: 2).PerformAsync(cancellation.Token));

        using var ctx = CreateDbContext();
        var filled = await ctx.BaseItems.AsNoTracking().CountAsync(e => e.CleanOriginalTitle != null, TestContext.Current.CancellationToken);
        Assert.InRange(filled, 1, 39);
        Assert.Equal(0, filled % 2);

        _log.OnMessage = null;
        await CreateRoutine(batchSize: 2).PerformAsync(TestContext.Current.CancellationToken);
        Assert.Equal(40, await ctx.BaseItems.AsNoTracking().CountAsync(e => e.CleanOriginalTitle != null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Perform_ManyBatches_LogsProgressWithoutTheValues()
    {
        for (var i = 0; i < 30; i++)
        {
            await AddItemAsync("Secret " + i, null);
        }

        await CreateRoutine(batchSize: 1).PerformAsync(TestContext.Current.CancellationToken);

        Assert.Contains(_log.Entries, e => e.Level == LogLevel.Information);
        Assert.DoesNotContain(_log.Entries, e => e.Message.Contains("Secret", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Perform_EmptyDatabase_DoesNothing()
    {
        await CreateRoutine().PerformAsync(TestContext.Current.CancellationToken);

        Assert.Empty(_commands.Updates);
        Assert.DoesNotContain(_log.Entries, e => e.Level >= LogLevel.Warning);
    }

    private BackfillNormalisedMatchColumns CreateRoutine(int batchSize = 1000)
    {
        return new BackfillNormalisedMatchColumns(
            new StartupLogger<BackfillNormalisedMatchColumns>(_log),
            CreateDbContextFactory(),
            batchSize);
    }

    private async Task<Guid> AddItemAsync(string? originalTitle, string? cleanOriginalTitle)
    {
        var id = Guid.NewGuid();
        using var ctx = CreateDbContext();
        ctx.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = "MediaBrowser.Controller.Entities.Movies.Movie",
            Name = "Item",
            OriginalTitle = originalTitle,
            CleanOriginalTitle = cleanOriginalTitle
        });
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        return id;
    }

    private async Task<Guid> AddPersonAsync(string name, string nameLower)
    {
        var id = Guid.NewGuid();
        using var ctx = CreateDbContext();
        ctx.Peoples.Add(new People { Id = id, Name = name, NameLower = nameLower, PersonType = "Actor" });
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        return id;
    }

    private async Task<string?> ReadItemAsync(Guid id)
    {
        using var ctx = CreateDbContext();
        return await ctx.BaseItems.AsNoTracking().Where(e => e.Id.Equals(id)).Select(e => e.CleanOriginalTitle).SingleAsync(TestContext.Current.CancellationToken);
    }

    private async Task<string> ReadPersonAsync(Guid id)
    {
        using var ctx = CreateDbContext();
        return await ctx.Peoples.AsNoTracking().Where(e => e.Id.Equals(id)).Select(e => e.NameLower).SingleAsync(TestContext.Current.CancellationToken);
    }

    private sealed class CommandLog : DbCommandInterceptor
    {
        public List<string> Updates { get; } = [];

        public int? FailOnUpdateNumber { get; set; }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                Updates.Add(command.CommandText);
                if (Updates.Count == FailOnUpdateNumber)
                {
                    throw new InvalidOperationException("The update was made to fail.");
                }
            }

            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class TransactionLog : DbTransactionInterceptor
    {
        public int Commits { get; set; }

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(
            DbTransaction transaction,
            TransactionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            Commits++;
            return base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public Action<LogLevel, string>? OnMessage { get; set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            Entries.Add((logLevel, message));
            OnMessage?.Invoke(logLevel, message);
        }
    }
}
