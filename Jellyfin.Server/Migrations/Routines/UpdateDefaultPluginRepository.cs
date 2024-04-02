using System;
using MediaBrowser.Controller.Configuration;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to update the default Jellyfin plugin repository.
/// </summary>
public class UpdateDefaultPluginRepository : IMigrationRoutine
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
    public Guid Id => new("852816E0-2712-49A9-9240-C6FC5FCAD1A8");

    /// <inheritdoc />
    public string Name => "UpdateDefaultPluginRepository10.9";

    /// <inheritdoc />
    public bool PerformOnNewInstall => true;

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
