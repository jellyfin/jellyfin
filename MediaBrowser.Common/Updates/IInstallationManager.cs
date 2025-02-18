using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Common.Updates;

/// <summary>
/// Defines the <see cref="IInstallationManager" />.
/// </summary>
public interface IInstallationManager : IDisposable
{
    /// <summary>
    /// Gets the completed installations.
    /// </summary>
    IEnumerable<InstallationInfo> CompletedInstallations { get; }

    /// <summary>
    /// Parses a plugin manifest at the supplied URL.
    /// </summary>
    /// <param name="manifestName">Name of the repository.</param>
    /// <param name="manifest">The URL to query.</param>
    /// <param name="filterIncompatible">Filter out incompatible plugins.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task{IReadOnlyList{PackageInfo}}.</returns>
    Task<PackageInfo[]> GetPackages(string manifestName, string manifest, bool filterIncompatible, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available packages that are supported by this version.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task{IReadOnlyList{PackageInfo}}.</returns>
    Task<IReadOnlyList<PackageInfo>> GetAvailablePackages(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all plugins matching the requirements.
    /// </summary>
    /// <param name="availablePackages">The available packages.</param>
    /// <param name="name">The name of the plugin.</param>
    /// <param name="id">The id of the plugin.</param>
    /// <param name="specificVersion">The version of the plugin.</param>
    /// <returns>All plugins matching the requirements.</returns>
    IEnumerable<PackageInfo> FilterPackages(
        IEnumerable<PackageInfo> availablePackages,
        string? name = null,
        Guid id = default,
        Version? specificVersion = null);

    /// <summary>
    /// Returns all compatible versions ordered from newest to oldest.
    /// </summary>
    /// <param name="availablePackages">The available packages.</param>
    /// <param name="name">The name.</param>
    /// <param name="id">The id of the plugin.</param>
    /// <param name="minVersion">The minimum required version of the plugin.</param>
    /// <param name="specificVersion">The specific version of the plugin to install.</param>
    /// <returns>All compatible versions ordered from newest to oldest.</returns>
    IEnumerable<InstallationInfo> GetCompatibleVersions(
        IEnumerable<PackageInfo> availablePackages,
        string? name = null,
        Guid id = default,
        Version? minVersion = null,
        Version? specificVersion = null);

    /// <summary>
    /// Returns the available compatible plugin updates.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The available plugin updates.</returns>
    Task<IEnumerable<InstallationInfo>> GetAvailablePluginUpdates(CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs the package.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see cref="Task" />.</returns>
    Task InstallPackage(InstallationInfo package, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstalls a plugin.
    /// </summary>
    /// <param name="plugin">The plugin.</param>
    void UninstallPlugin(LocalPlugin plugin);

    /// <summary>
    /// Cancels the installation.
    /// </summary>
    /// <param name="id">The id of the package that is being installed.</param>
    /// <returns>Returns true if the install was cancelled.</returns>
    bool CancelInstallation(Guid id);
}
