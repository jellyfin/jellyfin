using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Implementations;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Fills in the columns that hold the normalized original title of an item and the lower case name of a person, which
/// the schema migration adds empty. The values are computed in code, as they are whenever a row is stored, a batch of
/// rows at a time, and rows that already hold the right value are not written.
/// </summary>
[JellyfinMigration("2026-10-13T13:00:00", nameof(BackfillNormalisedMatchColumns))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class BackfillNormalisedMatchColumns : IAsyncMigrationRoutine
{
    private const int DefaultBatchSize = 1000;
    private const int ProgressBatchInterval = 10;

    private readonly IStartupLogger<BackfillNormalisedMatchColumns> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
    private readonly int _batchSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackfillNormalisedMatchColumns"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public BackfillNormalisedMatchColumns(
        IStartupLogger<BackfillNormalisedMatchColumns> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory)
        : this(logger, dbContextFactory, DefaultBatchSize)
    {
    }

    internal BackfillNormalisedMatchColumns(
        IStartupLogger<BackfillNormalisedMatchColumns> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory,
        int batchSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _batchSize = batchSize;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            await BackfillAsync(
                "items",
                context,
                after =>
                {
                    // Rows without an original title and without a value hold what they should.
                    var rows = context.BaseItems
                        .AsNoTracking()
                        .Where(e => e.OriginalTitle != null || e.CleanOriginalTitle != null);
                    if (after is { } last)
                    {
                        rows = rows.Where(e => e.Id.CompareTo(last) > 0);
                    }

                    return rows
                        .OrderBy(e => e.Id)
                        .Take(_batchSize)
                        .Select(e => new Row(e.Id, e.OriginalTitle, e.CleanOriginalTitle));
                },
                row => MatchNormalizer.CleanOriginalTitle(row.Source),
                (id, value, token) => context.BaseItems
                    .Where(e => e.Id.Equals(id))
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.CleanOriginalTitle, value), token),
                cancellationToken).ConfigureAwait(false);

            await BackfillAsync(
                "people",
                context,
                after =>
                {
                    var rows = context.Peoples.AsNoTracking();
                    if (after is { } last)
                    {
                        rows = rows.Where(e => e.Id.CompareTo(last) > 0);
                    }

                    return rows
                        .OrderBy(e => e.Id)
                        .Take(_batchSize)
                        .Select(e => new Row(e.Id, e.Name, e.NameLower));
                },
                row => MatchNormalizer.NameLower(row.Source!),
                (id, value, token) => context.Peoples
                    .Where(e => e.Id.Equals(id))
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.NameLower, value!), token),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task BackfillAsync(
        string table,
        JellyfinDbContext context,
        Func<Guid?, IQueryable<Row>> nextBatch,
        Func<Row, string?> compute,
        Func<Guid, string?, CancellationToken, Task<int>> write,
        CancellationToken cancellationToken)
    {
        Guid? after = null;
        long processed = 0;
        long changed = 0;
        var batches = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Paging by the key moves past every row of the batch, whether or not its value changes.
            var batch = await nextBatch(after).ToListAsync(cancellationToken).ConfigureAwait(false);
            if (batch.Count == 0)
            {
                break;
            }

            var updates = new List<(Guid Id, string? Value)>();
            foreach (var row in batch)
            {
                var value = compute(row);
                if (!string.Equals(value, row.Current, StringComparison.Ordinal))
                {
                    updates.Add((row.Id, value));
                }
            }

            if (updates.Count > 0)
            {
                var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                await using (transaction.ConfigureAwait(false))
                {
                    foreach (var (id, value) in updates)
                    {
                        await write(id, value, cancellationToken).ConfigureAwait(false);
                    }

                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            after = batch[^1].Id;
            processed += batch.Count;
            changed += updates.Count;
            batches++;

            if (batches % ProgressBatchInterval == 0)
            {
                _logger.LogInformation("Checked {Processed} {Table} so far, {Changed} needed their match columns filled in", processed, table, changed);
            }
        }

        if (changed > 0)
        {
            _logger.LogInformation("Filled in the match columns of {Changed} of {Processed} {Table}", changed, processed, table);
        }
    }

    private sealed record Row(Guid Id, string? Source, string? Current);
}
