#pragma warning disable RS0030 // Do not use banned APIs

using System;
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
            var values = await LegacyItemValues
                .ReadValuesAsync(context, [LegacyItemValues.Tag], cancellationToken)
                .ConfigureAwait(false);

            if (values.Count == 0)
            {
                _logger.LogInformation("No tag values to convert.");
                return;
            }

            _logger.LogInformation("Converting {Count} tag values into item tags.", values.Count);

            // Keyed as the table is, so a rerun after a part-way failure adds nothing twice.
            var written = (await context.BaseItemTags
                    .AsNoTracking()
                    .Select(t => new { t.ItemId, t.Value })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false))
                .Select(t => (t.ItemId, t.Value))
                .ToHashSet();

            var added = 0;

            // A chunk at a time, because a value has a link for every item carrying it.
            foreach (var chunk in values.Chunk(LegacyItemValues.ValueChunkSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var links = await LegacyItemValues.ReadLinksAsync(context, chunk, cancellationToken).ConfigureAwait(false);
                foreach (var link in links)
                {
                    if (!written.Add((link.ItemId, link.Value.Value)))
                    {
                        continue;
                    }

                    context.BaseItemTags.Add(new BaseItemTag
                    {
                        Item = null!,
                        ItemId = link.ItemId,
                        Value = link.Value.Value,
                        CleanValue = link.Value.CleanValue
                    });

                    added++;
                }

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.ChangeTracker.Clear();
            }

            _logger.LogInformation("Wrote {Tags} item tags.", added);

            // Last, so a failure part-way leaves the values to try again from.
            var removed = await LegacyItemValues
                .DeleteAsync(context, [LegacyItemValues.Tag], cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Removed {Values} tag values.", removed);
        }
    }
}
