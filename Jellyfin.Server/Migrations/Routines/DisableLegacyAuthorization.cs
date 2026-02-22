using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to disable legacy authorization in the system config.
/// </summary>
[JellyfinMigration("2025-11-18T16:00:00", nameof(DisableLegacyAuthorization))]
public class DisableLegacyAuthorization : IAsyncMigrationRoutine
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
    public Task PerformAsync(CancellationToken cancellationToken)
    {
        _serverConfigurationManager.Configuration.EnableLegacyAuthorization = false;
        _serverConfigurationManager.SaveConfiguration();

        return Task.CompletedTask;
    }
}
