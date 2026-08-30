using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Extensions;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to populate the pre-transliteration sort-name initial for existing library items.
/// </summary>
[JellyfinMigration("2026-08-29T19:00:00", nameof(RefreshSortNameInitials))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class RefreshSortNameInitials : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<RefreshSortNameInitials> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IServerConfigurationManager _configurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshSortNameInitials"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbProvider">The database context factory.</param>
    /// <param name="configurationManager">The server configuration manager.</param>
    public RefreshSortNameInitials(
        IStartupLogger<RefreshSortNameInitials> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IServerConfigurationManager configurationManager)
    {
        _logger = logger;
        _dbProvider = dbProvider;
        _configurationManager = configurationManager;
    }

    /// <inheritdoc />
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        const int Limit = 10000;
        var updatedCount = 0;
        var processedInPartition = 0;
        var configuration = _configurationManager.Configuration;
        var personType = typeof(Person).ToString();
        var sw = Stopwatch.StartNew();

        using var context = _dbProvider.CreateDbContext();
        var records = context.BaseItems.Count();
        _logger.LogInformation("Refreshing SortNameInitial for {Count} library items", records);

        await foreach (var item in context.BaseItems
                          .OrderBy(e => e.Id)
                          .WithPartitionProgress(partition => _logger.LogInformation(
                              "Processed: {Offset}/{Total} - Updated: {UpdatedCount} - Time: {Elapsed}",
                              partition * Limit,
                              records,
                              updatedCount,
                              sw.Elapsed))
                          .PartitionEagerAsync(Limit, cancellationToken)
                          .WithCancellation(cancellationToken)
                          .ConfigureAwait(false))
        {
            var enableAlphaNumericSorting = !string.Equals(item.Type, personType, StringComparison.Ordinal);
            var sourceName = !string.IsNullOrEmpty(item.ForcedSortName) ? item.ForcedSortName : item.Name;
            var newInitial = BaseItem.GetSortNameInitial(sourceName, enableAlphaNumericSorting, configuration);

            if (!string.Equals(newInitial, item.SortNameInitial, StringComparison.Ordinal))
            {
                item.SortNameInitial = newInitial;
                updatedCount++;
            }

            processedInPartition++;
            if (processedInPartition >= Limit)
            {
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.ChangeTracker.Clear();
                processedInPartition = 0;
            }
        }

        if (processedInPartition > 0)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            context.ChangeTracker.Clear();
        }

        _logger.LogInformation(
            "Refreshed SortNameInitial for {UpdatedCount} out of {TotalCount} items in {Time}",
            updatedCount,
            records,
            sw.Elapsed);
    }
}
