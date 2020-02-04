#pragma warning disable CS1591
#pragma warning disable SA1600

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
        event EventHandler<InstallationEventArgs> PackageInstalling;

        event EventHandler<InstallationEventArgs> PackageInstallationCompleted;

        event EventHandler<InstallationFailedEventArgs> PackageInstallationFailed;

        event EventHandler<InstallationEventArgs> PackageInstallationCancelled;

        /// <summary>
        /// Occurs when a plugin is uninstalled.
        /// </summary>
        event EventHandler<GenericEventArgs<IPlugin>> PluginUninstalled;

        /// <summary>
        /// Occurs when a plugin is updated.
        /// </summary>
        event EventHandler<GenericEventArgs<(IPlugin, PackageVersionInfo)>> PluginUpdated;

        /// <summary>
        /// Occurs when a plugin is installed.
        /// </summary>
        event EventHandler<GenericEventArgs<PackageVersionInfo>> PluginInstalled;

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
        /// <param name="availableVersions">The available version of the plugin.</param>
        /// <param name="minVersion">The minimum required version of the plugin.</param>
        /// <param name="classification">The classification of updates.</param>
        /// <returns>All compatible versions ordered from newest to oldest.</returns>
        IEnumerable<PackageVersionInfo> GetCompatibleVersions(
            IEnumerable<PackageVersionInfo> availableVersions,
            Version minVersion = null,
            PackageVersionClass classification = PackageVersionClass.Release);

        /// <summary>
        /// Returns all compatible versions ordered from newest to oldest.
        /// </summary>
        /// <param name="availablePackages">The available packages.</param>
        /// <param name="name">The name.</param>
        /// <param name="guid">The guid of the plugin.</param>
        /// <param name="minVersion">The minimum required version of the plugin.</param>
        /// <param name="classification">The classification.</param>
        /// <returns>All compatible versions ordered from newest to oldest.</returns>
        IEnumerable<PackageVersionInfo> GetCompatibleVersions(
            IEnumerable<PackageInfo> availablePackages,
            string name = null,
            Guid guid = default,
            Version minVersion = null,
            PackageVersionClass classification = PackageVersionClass.Release);

        /// <summary>
        /// Returns the available plugin updates.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The available plugin updates.</returns>
        IAsyncEnumerable<PackageVersionInfo> GetAvailablePluginUpdates(CancellationToken cancellationToken = default);

        /// <summary>
        /// Installs the package.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><see cref="Task" />.</returns>
        Task InstallPackage(PackageVersionInfo package, CancellationToken cancellationToken = default);

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
