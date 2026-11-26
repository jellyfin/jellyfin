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
/// Removes the duplicate home section rows left behind by earlier versions. Saving the display preferences
/// replaced the sections on a detached entity, so the previous rows were kept and a new set was inserted on
/// every save.
/// </summary>
[JellyfinMigration("2026-10-22T12:00:00", nameof(DeduplicateHomeSections))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class DeduplicateHomeSections : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<DeduplicateHomeSections> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeduplicateHomeSections"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public DeduplicateHomeSections(
        IStartupLogger<DeduplicateHomeSections> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // A later save inserts rows with a higher id, so the newest row of a position is the one the user
        // saved last and the older ones can go.
        var homeSections = await context.Set<HomeSection>()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var duplicates = homeSections
            .GroupBy(section => new { section.DisplayPreferencesId, section.Order })
            .SelectMany(group => group.OrderByDescending(section => section.Id).Skip(1))
            .ToList();

        if (duplicates.Count == 0)
        {
            _logger.LogInformation("No duplicate home sections found.");
            return;
        }

        context.Set<HomeSection>().RemoveRange(duplicates);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Removed {Count} duplicate home sections.", duplicates.Count);
    }
}
