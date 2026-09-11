using System;
using System.Collections.Generic;
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

            ResolvePrimaries(primaryByChild);

            var repaired = 0;
            var promoted = 0;

            // The primaries are loaded along with their versions: a primary that carries a
            // PrimaryVersionId of its own hides the whole group it heads.
            var itemIds = primaryByChild.Keys.Concat(primaryByChild.Values).Distinct().ToList();
            for (var offset = 0; offset < itemIds.Count; offset += BatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = itemIds.GetRange(offset, Math.Min(BatchSize, itemIds.Count - offset));
                var items = await dbContext.BaseItems
                    .Where(e => batch.Contains(e.Id))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                foreach (var item in items)
                {
                    if (primaryByChild.TryGetValue(item.Id, out var primaryId))
                    {
                        // Mirrors Video.CreatePresentationUniqueKey for a video that has a primary.
                        var expectedKey = primaryId.ToString("N", CultureInfo.InvariantCulture);
                        if (item.PrimaryVersionId.HasValue
                            && primaryId.Equals(item.PrimaryVersionId.Value)
                            && string.Equals(item.PresentationUniqueKey, expectedKey, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        item.PrimaryVersionId = primaryId;
                        item.PresentationUniqueKey = expectedKey;
                        repaired++;
                    }
                    else if (item.PrimaryVersionId.HasValue)
                    {
                        if (item.OwnerId.HasValue)
                        {
                            // An owned item is hidden by its owner rather than by its primary, so
                            // clearing the primary here would not bring the group back.
                            _logger.LogWarning(
                                "Alternate versions are linked to {ItemId}, which is owned by {OwnerId}; the group stays hidden until the owner is repaired.",
                                item.Id,
                                item.OwnerId.Value);
                            continue;
                        }

                        // Nothing links this one as a version, so the leftover primary is stale and
                        // would hide it, and with it every version linked to it.
                        _logger.LogWarning(
                            "Clearing the stale primary {PrimaryVersionId} of {ItemId}, which other versions are linked to.",
                            item.PrimaryVersionId.Value,
                            item.Id);

                        item.PrimaryVersionId = null;
                        item.PresentationUniqueKey = item.Id.ToString("N", CultureInfo.InvariantCulture);
                        promoted++;
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Repaired {Repaired} of {Total} alternate version links, and promoted {Promoted} primaries that were versions themselves.",
                repaired,
                primaryByChild.Count,
                promoted);
        }
    }

    private void ResolvePrimaries(Dictionary<Guid, Guid> primaryByChild)
    {
        var resolvedPrimaries = new Dictionary<Guid, Guid>(primaryByChild.Count);

        foreach (var start in primaryByChild.Keys.ToList())
        {
            if (resolvedPrimaries.ContainsKey(start))
            {
                continue;
            }

            var chain = new List<Guid>();
            var walked = new HashSet<Guid>();
            var current = start;
            Guid primary;

            while (true)
            {
                if (resolvedPrimaries.TryGetValue(current, out var resolved))
                {
                    primary = resolved;
                    break;
                }

                if (!primaryByChild.TryGetValue(current, out var next))
                {
                    // Nothing is linking this one as a version of something else, so it heads the group.
                    primary = current;
                    break;
                }

                if (!walked.Add(current))
                {
                    var loop = chain.Skip(chain.IndexOf(current)).ToList();

                    // Which member heads the group is arbitrary; the lowest id keeps the repair
                    // stable if the migration is ever re-run over the same data.
                    primary = loop.Min();
                    _logger.LogWarning(
                        "Alternate version links form a loop ({Loop}); keeping {PrimaryId} as the primary of the group.",
                        string.Join(" -> ", loop),
                        primary);

                    primaryByChild.Remove(primary);
                    break;
                }

                chain.Add(current);
                current = next;
            }

            foreach (var version in chain)
            {
                resolvedPrimaries[version] = primary;
            }
        }

        foreach (var (version, primary) in resolvedPrimaries)
        {
            if (primary.Equals(version))
            {
                // The member of a loop that was kept as the primary of its group.
                continue;
            }

            primaryByChild[version] = primary;
        }
    }
}
