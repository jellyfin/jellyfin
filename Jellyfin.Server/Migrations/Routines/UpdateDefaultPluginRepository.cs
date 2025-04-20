using System;
using MediaBrowser.Controller.Configuration;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to update the default Jellyfin plugin repository.
/// </summary>
[JellyfinMigration("2025-04-20T17:00:00", nameof(UpdateDefaultPluginRepository), "852816E0-2712-49A9-9240-C6FC5FCAD1A8", RunMigrationOnSetup = true)]
#pragma warning disable CS0618 // Type or member is obsolete
public class UpdateDefaultPluginRepository : IMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
{
    private const string NewRepositoryUrl = "https://repo.jellyfin.org/files/plugin/manifest.json";
    private const string OldRepositoryUrl = "https://repo.jellyfin.org/releases/plugin/manifest-stable.json";

    private readonly IServerConfigurationManager _serverConfigurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDefaultPluginRepository"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    public UpdateDefaultPluginRepository(IServerConfigurationManager serverConfigurationManager)
    {
        _serverConfigurationManager = serverConfigurationManager;
    }

    /// <inheritdoc />
    public void Perform()
    {
        var updated = false;
        foreach (var repo in _serverConfigurationManager.Configuration.PluginRepositories)
        {
            if (string.Equals(repo.Url, OldRepositoryUrl, StringComparison.OrdinalIgnoreCase))
            {
                repo.Url = NewRepositoryUrl;
                updated = true;
            }
        }

        if (updated)
        {
            _serverConfigurationManager.SaveConfiguration();
        }
    }
}
