using MediaBrowser.Model.Configuration;

namespace Jellyfin.Server.Configuration;

/// <summary>
/// Defines types for usage with the <see cref="StartupOptions.StartupMode"/>.
/// </summary>
public enum StartupMode
{
    /// <summary>
    /// Default startup mode, runs the jellyfin server in normal operation.
    /// </summary>
    MediaServer = 0,

    /// <summary>
    /// Attempts to Migrate the system only then shuts down.
    /// </summary>
    MigrateSystem = 1,

    /// <summary>
    /// Runs the Database seed function regardless of <see cref="BaseApplicationConfiguration.IsStartupWizardCompleted"/> state.
    /// </summary>
    SeedSystem = 2
}
