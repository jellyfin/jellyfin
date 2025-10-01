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
        ILogger<FixDates> logger,
        IStartupLogger<FixDates> startupLogger,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _logger = startupLogger.With(logger);
        _dbProvider = dbProvider;
    }

    /// <inheritdoc />
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        if (!TimeZoneInfo.Local.Equals(TimeZoneInfo.Utc))
        {
            var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using (context.ConfigureAwait(false))
            {
                var sw = Stopwatch.StartNew();

                await FixBaseItemsAsync(context, sw, cancellationToken).ConfigureAwait(false);
                sw.Reset();
                await FixChaptersAsync(context, sw, cancellationToken).ConfigureAwait(false);
                sw.Reset();
                await FixBaseItemImageInfos(context, sw, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task FixBaseItemsAsync(JellyfinDbContext context, Stopwatch sw, CancellationToken cancellationToken)
    {
        int itemCount = 0;

        var baseQuery = context.BaseItems.OrderBy(e => e.Id);
        var records = baseQuery.Count();
        _logger.LogInformation("Fixing dates for {Count} BaseItems.", records);

        sw.Start();
        await foreach (var result in context.BaseItems.OrderBy(e => e.Id)
                        .WithPartitionProgress(
                            (partition) =>
                                _logger.LogInformation(
                                    "Processing BaseItems batch {BatchNumber} ({ProcessedSoFar}/{TotalRecords}) - Time: {ElapsedTime}",
                                    partition + 1,
                                    Math.Min((partition + 1) * PageSize, records),
                                    records,
                                    sw.Elapsed))
                        .PartitionEagerAsync(PageSize, cancellationToken)
                        .WithCancellation(cancellationToken)
                        .ConfigureAwait(false))
        {
            result.DateCreated = ToUniversalTime(result.DateCreated);
            result.DateLastMediaAdded = ToUniversalTime(result.DateLastMediaAdded);
            result.DateLastRefreshed = ToUniversalTime(result.DateLastRefreshed);
            result.DateLastSaved = ToUniversalTime(result.DateLastSaved);
            result.DateModified = ToUniversalTime(result.DateModified);
            itemCount++;
        }

        var saveCount = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("BaseItems: Processed {ItemCount} items, saved {SaveCount} changes in {ElapsedTime}", itemCount, saveCount, sw.Elapsed);
    }

    private async Task FixChaptersAsync(JellyfinDbContext context, Stopwatch sw, CancellationToken cancellationToken)
    {
        int itemCount = 0;

        var baseQuery = context.Chapters;
        var records = baseQuery.Count();
        _logger.LogInformation("Fixing dates for {Count} Chapters.", records);

        sw.Start();
        await foreach (var result in context.Chapters.OrderBy(e => e.ItemId)
                        .WithPartitionProgress(
                            (partition) =>
                                _logger.LogInformation(
                                    "Processing Chapter batch {BatchNumber} ({ProcessedSoFar}/{TotalRecords}) - Time: {ElapsedTime}",
                                    partition + 1,
                                    Math.Min((partition + 1) * PageSize, records),
                                    records,
                                    sw.Elapsed))
                        .PartitionEagerAsync(PageSize, cancellationToken)
                        .WithCancellation(cancellationToken)
                        .ConfigureAwait(false))
        {
            result.ImageDateModified = ToUniversalTime(result.ImageDateModified, true);
            itemCount++;
        }

        var saveCount = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Chapters: Processed {ItemCount} items, saved {SaveCount} changes in {ElapsedTime}", itemCount, saveCount, sw.Elapsed);
    }

    private async Task FixBaseItemImageInfos(JellyfinDbContext context, Stopwatch sw, CancellationToken cancellationToken)
    {
        int itemCount = 0;

        var baseQuery = context.BaseItemImageInfos;
        var records = baseQuery.Count();
        _logger.LogInformation("Fixing dates for {Count} BaseItemImageInfos.", records);

        sw.Start();
        await foreach (var result in context.BaseItemImageInfos.OrderBy(e => e.Id)
                        .WithPartitionProgress(
                            (partition) =>
                                _logger.LogInformation(
                                    "Processing BaseItemImageInfos batch {BatchNumber} ({ProcessedSoFar}/{TotalRecords}) - Time: {ElapsedTime}",
                                    partition + 1,
                                    Math.Min((partition + 1) * PageSize, records),
                                    records,
                                    sw.Elapsed))
                        .PartitionEagerAsync(PageSize, cancellationToken)
                        .WithCancellation(cancellationToken)
                        .ConfigureAwait(false))
        {
            result.DateModified = ToUniversalTime(result.DateModified);
            itemCount++;
        }

        var saveCount = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("BaseItemImageInfos: Processed {ItemCount} items, saved {SaveCount} changes in {ElapsedTime}", itemCount, saveCount, sw.Elapsed);
    }

    private DateTime? ToUniversalTime(DateTime? dateTime, bool isUTC = false)
    {
        if (dateTime is null)
        {
            return null;
        }

        if (dateTime.Value.Year == 1 && dateTime.Value.Month == 1 && dateTime.Value.Day == 1)
        {
            return null;
        }

        if (dateTime.Value.Kind == DateTimeKind.Utc || isUTC)
        {
            return dateTime.Value;
        }

        return dateTime.Value.ToUniversalTime();
    }
}
