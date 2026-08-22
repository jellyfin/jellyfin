#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Turns the tag values an item carries into rows of its own, then removes them.
/// </summary>
[JellyfinMigration("2026-08-15T00:00:00", nameof(ConvertTagValuesToItemTags))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class ConvertTagValuesToItemTags : IAsyncMigrationRoutine
{
    private const int BatchSize = 500;

    private readonly IStartupLogger<ConvertTagValuesToItemTags> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConvertTagValuesToItemTags"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public ConvertTagValuesToItemTags(
        IStartupLogger<ConvertTagValuesToItemTags> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var values = await context.ItemValues
                .AsNoTracking()
                .Where(v => v.Type == ItemValueType.Tags)
                .Select(v => new { v.ItemValueId, v.Value, v.CleanValue })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (values.Count == 0)
            {
                _logger.LogInformation("No tag values to convert.");
                return;
            }

            _logger.LogInformation("Converting {Count} tag values into item tags.", values.Count);

            // Keyed as the table is, so a rerun after a part-way failure adds nothing twice.
            var existingTags = (await context.BaseItemTags
                    .AsNoTracking()
                    .Select(t => new { t.ItemId, t.Value })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false))
                .Select(t => (t.ItemId, t.Value))
                .ToHashSet();

            var written = 0;

            foreach (var batch in values.Chunk(BatchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var valueIds = batch.Select(v => v.ItemValueId).ToArray();
                var maps = (await context.ItemValuesMap
                        .AsNoTracking()
                        .Where(m => valueIds.Contains(m.ItemValueId))
                        .Select(m => new { m.ItemId, m.ItemValueId })
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false))
                    .ToLookup(m => m.ItemValueId, m => m.ItemId);

                if (maps.Count == 0)
                {
                    continue;
                }

                foreach (var value in batch)
                {
                    foreach (var itemId in maps[value.ItemValueId])
                    {
                        if (!existingTags.Add((itemId, value.Value)))
                        {
                            continue;
                        }

                        context.BaseItemTags.Add(new BaseItemTag
                        {
                            Item = null!,
                            ItemId = itemId,
                            Value = value.Value,
                            CleanValue = value.CleanValue
                        });

                        written++;
                    }
                }

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Wrote {Tags} item tags.", written);

            // Last, so a failure part-way leaves the values to try again from.
            var removedMaps = await context.ItemValuesMap
                .Where(m => m.ItemValue.Type == ItemValueType.Tags)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
            var removedValues = await context.ItemValues
                .Where(v => v.Type == ItemValueType.Tags)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Removed {Values} tag values and {Maps} of their item links.", removedValues, removedMaps);
        }
    }
}
