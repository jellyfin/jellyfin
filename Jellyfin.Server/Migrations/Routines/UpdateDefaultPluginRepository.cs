using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to update the default Jellyfin plugin repository.
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
[JellyfinMigration("2025-04-20T17:00:00", nameof(UpdateDefaultPluginRepository), "852816E0-2712-49A9-9240-C6FC5FCAD1A8", RunMigrationOnSetup = true)]
public class UpdateDefaultPluginRepository : IMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
{
    private const string NewRepositoryUrl = "https://repo.jellyfin.org/files/plugin/manifest.json";
    private const string OldRepositoryUrl = "https://repo.jellyfin.org/releases/plugin/manifest-stable.json";

    private readonly IWritableOptions<ServerConfiguration> _serverConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDefaultPluginRepository"/> class.
    /// </summary>
    /// <param name="serverConfig">Instance of the server config.</param>
    public UpdateDefaultPluginRepository(IWritableOptions<ServerConfiguration> serverConfig)
    {
        _serverConfig = serverConfig;
    }

    /// <inheritdoc />
    public void Perform()
    {
        var updated = false;
        var repos = _serverConfig.Value.PluginRepositories;
        foreach (var repo in repos)
        {
            if (string.Equals(repo.Url, OldRepositoryUrl, StringComparison.OrdinalIgnoreCase))
            {
                repo.Url = NewRepositoryUrl;
                updated = true;
            }
        }

        if (updated)
        {
            _serverConfig.Update(c => c.PluginRepositories = repos);
        }
    }
}
