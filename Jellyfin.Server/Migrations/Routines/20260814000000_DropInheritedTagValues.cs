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
/// Removes the inherited tag values, which nothing reads.
/// </summary>
[JellyfinMigration("2026-08-14T00:00:00", nameof(DropInheritedTagValues))]
public class DropInheritedTagValues : IAsyncMigrationRoutine
{
    // Gone from ItemValueType, but the rows an older server wrote still carry the number.
    private const ItemValueType InheritedTags = (ItemValueType)6;

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
            var removedMaps = await context.ItemValuesMap
                .Where(m => m.ItemValue.Type == InheritedTags)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
            var removedValues = await context.ItemValues
                .Where(v => v.Type == InheritedTags)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Removed {Values} inherited tag values and {Maps} of their item links.", removedValues, removedMaps);
        }
    }
}
