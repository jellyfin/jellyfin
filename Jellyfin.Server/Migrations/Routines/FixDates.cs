using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to fix dates saved in the database to always be UTC.
/// </summary>
[JellyfinMigration("2025-06-20T18:00:00", nameof(FixDates))]
public class FixDates : IAsyncMigrationRoutine
{
    private const int PageSize = 5000;

    private readonly ILogger _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixDates"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="startupLogger">The startup logger for Startup UI integration.</param>
    /// <param name="dbProvider">Instance of the <see cref="IDbContextFactory{JellyfinDbContext}"/> interface.</param>
    public FixDates(
        ILogger<MoveExtractedFiles> logger,
        IStartupLogger<MoveExtractedFiles> startupLogger,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _logger = startupLogger.With(logger);
        _dbProvider = dbProvider;
    }

    /// <inheritdoc />
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        using var context = _dbProvider.CreateDbContext();
        var sw = Stopwatch.StartNew();

        await FixBaseItemsAsync(context, sw, cancellationToken).ConfigureAwait(false);
        sw.Reset();
        await FixChaptersAsync(context, sw, cancellationToken).ConfigureAwait(false);
        sw.Reset();
        await FixBaseItemImageInfos(context, sw, cancellationToken).ConfigureAwait(false);
    }

    private async Task FixBaseItemImageInfos(JellyfinDbContext context, Stopwatch sw, CancellationToken cancellationToken)
    {
        int offset = 0;

        var baseQuery = context.BaseItemImageInfos;
        var records = baseQuery.Count();
        _logger.LogInformation("Modifying dates for {Count} BaseItemImageInfos.", records);

        var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            do
            {
                var results = baseQuery.Skip(offset).Take(PageSize).ToList();
                foreach (var result in results)
                {
                    result.DateModified = ToUniversalTime(result.DateModified) ?? DateTimeOffset.MinValue.UtcDateTime;
                }

                offset += PageSize;
                if (offset > records)
                {
                    offset = records;
                }

                _logger.LogInformation("Modified: {Count} - Time: {Time}", offset, sw.Elapsed);
            } while (offset < records);

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Modified dates for {Count} BaseItemImageInfos in {Time}", offset, sw.Elapsed);
    }

    private async Task FixChaptersAsync(JellyfinDbContext context, Stopwatch sw, CancellationToken cancellationToken)
    {
        int offset = 0;

        var baseQuery = context.Chapters;
        var records = baseQuery.Count();
        _logger.LogInformation("Modifying dates for {Count} Chapters.", records);

        var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            do
            {
                var results = baseQuery.Skip(offset).Take(PageSize).ToList();
                foreach (var result in results)
                {
                    result.ImageDateModified = ToUniversalTime(result.ImageDateModified, true);
                }

                offset += PageSize;
                if (offset > records)
                {
                    offset = records;
                }

                _logger.LogInformation("Modified: {Count} - Time: {Time}", offset, sw.Elapsed);
            } while (offset < records);

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Modified dates for {Count} Chapters in {Time}", offset, sw.Elapsed);
    }

    private async Task FixBaseItemsAsync(JellyfinDbContext context, Stopwatch sw, CancellationToken cancellationToken)
    {
        const int Limit = 5000;
        int offset = 0;

        var baseQuery = context.BaseItems.OrderBy(e => e.Id);
        var records = baseQuery.Count();
        _logger.LogInformation("Modifying dates for {Count} BaseItems.", records);

        var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            do
            {
                var results = baseQuery.Skip(offset).Take(Limit).ToList();
                foreach (var result in results)
                {
                    result.DateCreated = ToUniversalTime(result.DateCreated);
                    result.DateLastMediaAdded = ToUniversalTime(result.DateLastMediaAdded);
                    result.DateLastRefreshed = ToUniversalTime(result.DateLastRefreshed);
                    result.DateLastSaved = ToUniversalTime(result.DateLastSaved);
                    result.DateModified = ToUniversalTime(result.DateModified);
                }

                offset += Limit;
                if (offset > records)
                {
                    offset = records;
                }

                _logger.LogInformation("Modified: {Count} - Time: {Time}", offset, sw.Elapsed);
            } while (offset < records);

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Modified dates for {Count} BaseItems in {Time}", offset, sw.Elapsed);
    }

    private DateTime? ToUniversalTime(DateTime? dateTime, bool isUtc = false)
    {
        if (dateTime == null)
        {
            return null;
        }

        if (dateTime == default)
        {
            return DateTimeOffset.MinValue.UtcDateTime;
        }

        return dateTime.Value.ToUniversalTime();
    }
}
