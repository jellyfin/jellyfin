using MediaBrowser.Controller.Configuration;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to disable legacy authorization in the system config.
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
[JellyfinMigration("2025-11-18T16:00:00", nameof(DisableLegacyAuthorization), "F020F843-E079-4061-99E0-F43D145F2557")]
public class DisableLegacyAuthorization : IMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly IServerConfigurationManager _serverConfigurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisableLegacyAuthorization"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    public DisableLegacyAuthorization(IServerConfigurationManager serverConfigurationManager)
    {
        _serverConfigurationManager = serverConfigurationManager;
    }

    /// <inheritdoc />
    public void Perform()
    {
        _serverConfigurationManager.Configuration.EnableLegacyAuthorization = false;
        _serverConfigurationManager.SaveConfiguration();
    }
}
