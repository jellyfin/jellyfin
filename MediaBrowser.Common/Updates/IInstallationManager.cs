#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Common.Updates
{
    public interface IInstallationManager : IDisposable
    {
        event EventHandler<InstallationInfo> PackageInstalling;

        event EventHandler<InstallationInfo> PackageInstallationCompleted;

        event EventHandler<InstallationFailedEventArgs> PackageInstallationFailed;

        event EventHandler<InstallationInfo> PackageInstallationCancelled;

        /// <summary>
        /// Occurs when a plugin is uninstalled.
        /// </summary>
        event EventHandler<IPlugin> PluginUninstalled;

        /// <summary>
        /// Occurs when a plugin is updated.
        /// </summary>
        event EventHandler<InstallationInfo> PluginUpdated;

        /// <summary>
        /// Occurs when a plugin is installed.
        /// </summary>
        event EventHandler<InstallationInfo> PluginInstalled;

        /// <summary>
        /// Gets the completed installations.
        /// </summary>
        IEnumerable<InstallationInfo> CompletedInstallations { get; }

        /// <summary>
        /// Gets all available packages.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IReadOnlyList{PackageInfo}}.</returns>
        Task<IReadOnlyList<PackageInfo>> GetAvailablePackages(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns all plugins matching the requirements.
        /// </summary>
        /// <param name="availablePackages">The available packages.</param>
        /// <param name="name">The name of the plugin.</param>
        /// <param name="guid">The id of the plugin.</param>
        /// <returns>All plugins matching the requirements.</returns>
        IEnumerable<PackageInfo> FilterPackages(
            IEnumerable<PackageInfo> availablePackages,
            string name = null,
            Guid guid = default);

        /// <summary>
        /// Returns all compatible versions ordered from newest to oldest.
        /// </summary>
        /// <param name="availablePackages">The available packages.</param>
        /// <param name="name">The name.</param>
        /// <param name="guid">The guid of the plugin.</param>
        /// <param name="minVersion">The minimum required version of the plugin.</param>
        /// <returns>All compatible versions ordered from newest to oldest.</returns>
        IEnumerable<InstallationInfo> GetCompatibleVersions(
            IEnumerable<PackageInfo> availablePackages,
            string name = null,
            Guid guid = default,
            Version minVersion = null);

        /// <summary>
        /// Returns the available plugin updates.
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
        void UninstallPlugin(IPlugin plugin);

        /// <summary>
        /// Cancels the installation.
        /// </summary>
        /// <param name="id">The id of the package that is being installed.</param>
        /// <returns>Returns true if the install was cancelled.</returns>
        bool CancelInstallation(Guid id);
    }
}
