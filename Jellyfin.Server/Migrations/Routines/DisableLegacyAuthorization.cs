using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to disable legacy authorization in the system config.
/// </summary>
[JellyfinMigration("2025-11-18T16:00:00", nameof(DisableLegacyAuthorization))]
public class DisableLegacyAuthorization : IAsyncMigrationRoutine
{
    private readonly IWritableOptions<ServerConfiguration> _serverConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisableLegacyAuthorization"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the server configuration.</param>
    public DisableLegacyAuthorization(IWritableOptions<ServerConfiguration> serverConfigurationManager)
    {
        _serverConfig = serverConfigurationManager;
    }

    /// <inheritdoc />
    public Task PerformAsync(CancellationToken cancellationToken)
    {
        _serverConfig.Update(value => value.EnableLegacyAuthorization = false);
        return Task.CompletedTask;
    }
}
