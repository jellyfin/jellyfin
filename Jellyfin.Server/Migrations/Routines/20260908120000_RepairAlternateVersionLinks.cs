using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Re-points every video that is linked as an alternate version at the primary it belongs to.
/// </summary>
[JellyfinMigration("2026-09-08T12:00:00", nameof(RepairAlternateVersionLinks))]
[JellyfinMigrationBackup(JellyfinDb = true)]
internal class RepairAlternateVersionLinks : IAsyncMigrationRoutine
{
    private const int BatchSize = 1000;

    private readonly IStartupLogger<RepairAlternateVersionLinks> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RepairAlternateVersionLinks"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbProvider">The database context factory.</param>
    public RepairAlternateVersionLinks(
        IStartupLogger<RepairAlternateVersionLinks> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _logger = logger;
        _dbProvider = dbProvider;
    }

    /// <inheritdoc />
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var links = await dbContext.LinkedChildren
                .Where(lc => lc.ChildType == LinkedChildType.LocalAlternateVersion
                    || lc.ChildType == LinkedChildType.LinkedAlternateVersion)
                .Select(lc => new { lc.ParentId, lc.ChildId, lc.ChildType })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (links.Count == 0)
            {
                _logger.LogInformation("No alternate version links found, nothing to repair.");
                return;
            }

            // A version belongs to one primary; a file-based link outranks a user-merged one, as it
            // does everywhere else these two link types meet.
            var primaryByChild = links
                .GroupBy(l => l.ChildId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(l => l.ChildType == LinkedChildType.LocalAlternateVersion ? 0 : 1)
                        .First()
                        .ParentId);

            // A version that is its own primary would be hidden from every list by the repair below.
            foreach (var selfLink in primaryByChild.Where(kvp => kvp.Value.Equals(kvp.Key)).ToList())
            {
                _logger.LogWarning("Skipping alternate version {ChildId}, which is linked to itself.", selfLink.Key);
                primaryByChild.Remove(selfLink.Key);
            }

            var repaired = 0;
            var childIds = primaryByChild.Keys.ToList();
            for (var offset = 0; offset < childIds.Count; offset += BatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = childIds.GetRange(offset, Math.Min(BatchSize, childIds.Count - offset));
                var children = await dbContext.BaseItems
                    .Where(e => batch.Contains(e.Id))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                foreach (var child in children)
                {
                    var primaryId = primaryByChild[child.Id];

                    // Mirrors Video.CreatePresentationUniqueKey for a video that has a primary.
                    var expectedKey = primaryId.ToString("N", CultureInfo.InvariantCulture);
                    if (child.PrimaryVersionId.HasValue
                        && primaryId.Equals(child.PrimaryVersionId.Value)
                        && string.Equals(child.PresentationUniqueKey, expectedKey, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    child.PrimaryVersionId = primaryId;
                    child.PresentationUniqueKey = expectedKey;
                    repaired++;
                }

                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Repaired {Repaired} of {Total} alternate version links.",
                repaired,
                primaryByChild.Count);
        }
    }
}
