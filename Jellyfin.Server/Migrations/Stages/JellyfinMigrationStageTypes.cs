namespace Jellyfin.Server.Migrations.Stages;

/// <summary>
/// Defines the stages the <see cref="JellyfinMigrationService"/> supports.
/// </summary>
internal enum JellyfinMigrationStageTypes
{
    /// <summary>
    /// Runs before services are initialised.
    /// </summary>
    PreInitialisation = 1,

    /// <summary>
    /// Runs after the host has been configured and includes the database migrations.
    /// </summary>
    CoreInitialisaition = 2
}
