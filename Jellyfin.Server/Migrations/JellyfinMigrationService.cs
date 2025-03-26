using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Migrations.Stages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Jellyfin.Server.Migrations;

/// <summary>
/// Handles Migration of the Jellyfin data structure.
/// </summary>
public class JellyfinMigrationService
{
    private const string ORDEROFENTRYMIGRATION = "9999/00/00:00.00Z";
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinMigrationService"/> class.
    /// </summary>
    /// <param name="dbContextFactory">Provides access to the jellyfin database.</param>
    public JellyfinMigrationService(IDbContextFactory<JellyfinDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private HashSet<MigrationStage> Migrations { get; } = new()
    {
          new MigrationStage(JellyfinMigrationStageTypes.PreInitialisation)
          {
            { new(ORDEROFENTRYMIGRATION, typeof(PreStartupRoutines.CreateNetworkConfiguration)) },
            { new(ORDEROFENTRYMIGRATION, typeof(PreStartupRoutines.MigrateMusicBrainzTimeout)) },
            { new(ORDEROFENTRYMIGRATION, typeof(PreStartupRoutines.MigrateMusicBrainzTimeout)) },
            { new(ORDEROFENTRYMIGRATION, typeof(PreStartupRoutines.MigrateEncodingOptions)) },
          },
          new CoreMigrationStage()
          {
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.DisableTranscodingThrottling)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.CreateUserLoggingConfigFile)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.MigrateActivityLogDb)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.RemoveDuplicateExtras)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.AddDefaultPluginRepository)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.MigrateUserDb)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.ReaddDefaultPluginRepository)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.MigrateDisplayPreferencesDb)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.RemoveDownloadImagesInAdvance)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.MigrateAuthenticationDb)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.FixPlaylistOwner)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.MigrateRatingLevels)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.AddDefaultCastReceivers)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.UpdateDefaultPluginRepository)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.FixAudioData)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.MoveTrickplayFiles)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.RemoveDuplicatePlaylistChildren)) },
            { new(ORDEROFENTRYMIGRATION, typeof(Routines.MigrateLibraryDb)) },
          }
    };

    public async Task MigrateStepAsync(JellyfinMigrationStageTypes stage, IServiceProvider? serviceProvider)
    {
        var migrationStage = Migrations.First(e => e.Stage == stage);
        var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {

        }
        migrationStage.ExecuteMigrationsAsync()
    }
}
