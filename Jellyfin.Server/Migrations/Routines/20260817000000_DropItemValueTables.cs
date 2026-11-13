using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Drops the value tables an older server wrote, once everything reading them has run.
/// </summary>
/// <remarks>
/// Runs on setup as well, because a fresh install creates the tables on its way through the migrations.
/// </remarks>
[JellyfinMigration(
    "2026-08-17T00:00:00",
    nameof(DropItemValueTables),
    RunMigrationOnSetup = true,
    Stage = Stages.JellyfinMigrationStageTypes.AppInitialisation)]
public class DropItemValueTables : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<DropItemValueTables> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DropItemValueTables"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public DropItemValueTables(
        IStartupLogger<DropItemValueTables> logger,
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
            await LegacyItemValues.DropTablesAsync(context, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Removed the legacy item value tables.");
    }
}
