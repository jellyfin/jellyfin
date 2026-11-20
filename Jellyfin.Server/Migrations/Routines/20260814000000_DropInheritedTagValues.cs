using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Removes the inherited tag values, which nothing reads.
/// </summary>
[JellyfinMigration("2026-08-14T00:00:00", nameof(DropInheritedTagValues))]
public class DropInheritedTagValues : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<DropInheritedTagValues> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DropInheritedTagValues"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public DropInheritedTagValues(
        IStartupLogger<DropInheritedTagValues> logger,
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
            var removed = await LegacyItemValues
                .DeleteAsync(context, [LegacyItemValues.InheritedTag], cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Removed {Values} inherited tag values.", removed);
        }
    }
}
