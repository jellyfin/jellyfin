namespace Jellyfin.Server.Migrations.Stages;

/// <summary>
/// Defines the stages the <see cref="JellyfinMigrationService"/> supports.
/// </summary>
#pragma warning disable CA1008 // Enums should have zero value
public enum JellyfinMigrationStageTypes
#pragma warning restore CA1008 // Enums should have zero value
{
    /// <summary>
    /// Runs before services are initialised.
    /// Reserved for migrations that are modifying the application server itself. Should be avoided if possible.
    /// </summary>
    PreInitialisation = 1,

    /// <summary>
    /// Runs after the host has been configured and includes the database migrations.
    /// Allows the mix order of migrations that contain application code and database changes.
    /// </summary>
    CoreInitialisaition = 2,

    /// <summary>
    /// Runs after services has been registered and initialised. Last step before running the server.
    /// </summary>
    AppInitialisation = 3
}
