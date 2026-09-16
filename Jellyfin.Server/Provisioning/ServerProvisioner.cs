using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Provisioning;

/// <summary>
/// Applies a <see cref="ProvisionManifest"/> to a server that has not been set up yet, performing
/// the same operations the startup wizard performs so the server can be brought up without a browser.
/// </summary>
public sealed class ServerProvisioner
{
    private readonly IServerConfigurationManager _configurationManager;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<ServerProvisioner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerProvisioner"/> class.
    /// </summary>
    /// <param name="configurationManager">The server configuration manager.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    public ServerProvisioner(
        IServerConfigurationManager configurationManager,
        IUserManager userManager,
        ILibraryManager libraryManager,
        ILogger<ServerProvisioner> logger)
    {
        _configurationManager = configurationManager;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Provisions the server, or does nothing if it has already been set up.
    /// </summary>
    /// <param name="manifest">The manifest to apply.</param>
    /// <returns>A task representing the provisioning operation.</returns>
    public async Task ProvisionAsync(ProvisionManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        // Re-running provisioning against a configured server would reset the administrator's
        // password, so configuration management that reruns on every rebuild gets a no-op instead.
        if (_configurationManager.Configuration.IsStartupWizardCompleted)
        {
            _logger.LogInformation("This server has already been set up. Nothing to provision.");
            return;
        }

        _logger.LogInformation("Provisioning server.");

        // Checked before anything is written so a manifest naming a path that is not mounted yet
        // leaves the server untouched and the run can simply be repeated.
        EnsureLibraryPathsExist(manifest.Libraries);

        ApplyServerConfiguration(manifest);
        ApplyRemoteAccess(manifest.EnableRemoteAccess);
        await ApplyAdministratorAsync(manifest.Administrator).ConfigureAwait(false);
        await ApplyLibrariesAsync(manifest.Libraries).ConfigureAwait(false);

        _configurationManager.Configuration.IsStartupWizardCompleted = true;
        _configurationManager.SaveConfiguration();

        _logger.LogInformation("Provisioning complete.");
    }

    private void ApplyServerConfiguration(ProvisionManifest manifest)
    {
        var configuration = _configurationManager.Configuration;

        if (manifest.ServerName is not null)
        {
            configuration.ServerName = manifest.ServerName;
        }

        if (manifest.UICulture is not null)
        {
            configuration.UICulture = manifest.UICulture;
        }

        if (manifest.MetadataCountryCode is not null)
        {
            configuration.MetadataCountryCode = manifest.MetadataCountryCode;
        }

        if (manifest.PreferredMetadataLanguage is not null)
        {
            configuration.PreferredMetadataLanguage = manifest.PreferredMetadataLanguage;
        }
    }

    private void ApplyRemoteAccess(bool? enableRemoteAccess)
    {
        if (enableRemoteAccess is null)
        {
            return;
        }

        var networkConfiguration = _configurationManager.GetNetworkConfiguration();
        networkConfiguration.EnableRemoteAccess = enableRemoteAccess.Value;
        _configurationManager.SaveConfiguration(NetworkConfigurationStore.StoreKey, networkConfiguration);
    }

    private async Task ApplyAdministratorAsync(ProvisionAdministrator administrator)
    {
        // InitializeAsync creates the default administrator the wizard then renames, which is
        // still the only supported way to get the initial account and its permissions.
        await _userManager.InitializeAsync().ConfigureAwait(false);
        var user = _userManager.GetFirstUser()
            ?? throw new InvalidOperationException("No user exists after initialization.");

        if (!string.Equals(user.Username, administrator.Name, StringComparison.OrdinalIgnoreCase))
        {
            await _userManager.RenameUser(user.Id, user.Username, administrator.Name).ConfigureAwait(false);
        }

        await _userManager.ChangePassword(user.Id, administrator.Password).ConfigureAwait(false);

        _logger.LogInformation("Created administrator {Name}.", administrator.Name);
    }

    private static void EnsureLibraryPathsExist(IReadOnlyList<ProvisionLibrary> libraries)
    {
        var missingPaths = libraries
            .SelectMany(library => library.Paths)
            .Where(path => !Directory.Exists(path))
            .ToArray();

        if (missingPaths.Length > 0)
        {
            throw new InvalidOperationException(
                "These library paths do not exist: " + string.Join(", ", missingPaths));
        }
    }

    private async Task ApplyLibrariesAsync(IReadOnlyList<ProvisionLibrary> libraries)
    {
        foreach (var library in libraries)
        {
            var libraryOptions = library.LibraryOptions ?? new LibraryOptions();
            libraryOptions.PathInfos = Array.ConvertAll(library.Paths.ToArray(), path => new MediaPathInfo(path));

            await _libraryManager.AddVirtualFolder(library.Name, library.CollectionType, libraryOptions, refreshLibrary: false).ConfigureAwait(false);

            _logger.LogInformation("Created library {Name}.", library.Name);
        }
    }
}
