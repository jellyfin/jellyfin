using System;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Updates;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to initialize system configuration with the default plugin repository.
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
[JellyfinMigration("2025-04-20T11:00:00", nameof(ReaddDefaultPluginRepository), "5F86E7F6-D966-4C77-849D-7A7B40B68C4E", RunMigrationOnSetup = true)]
public class ReaddDefaultPluginRepository : IMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly IServerConfigurationManager _serverConfigurationManager;

    private readonly RepositoryInfo _defaultRepositoryInfo = new RepositoryInfo
    {
        Name = "Jellyfin Stable",
        Url = "https://repo.jellyfin.org/releases/plugin/manifest-stable.json"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ReaddDefaultPluginRepository"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    public ReaddDefaultPluginRepository(IServerConfigurationManager serverConfigurationManager)
    {
        _serverConfigurationManager = serverConfigurationManager;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        // Only add if repository list is empty
        if (_serverConfigurationManager.Configuration.PluginRepositories.Length == 0)
        {
            _serverConfigurationManager.Configuration.PluginRepositories = new[] { _defaultRepositoryInfo };
            _serverConfigurationManager.SaveConfiguration();
        }
    }
}
