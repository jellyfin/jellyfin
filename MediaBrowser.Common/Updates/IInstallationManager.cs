using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Updates;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Updates
{
    public interface IInstallationManager : IDisposable
    {
        event EventHandler<InstallationEventArgs> PackageInstalling;
        event EventHandler<InstallationEventArgs> PackageInstallationCompleted;
        event EventHandler<InstallationFailedEventArgs> PackageInstallationFailed;
        event EventHandler<InstallationEventArgs> PackageInstallationCancelled;

        /// <summary>
        /// The current installations
        /// </summary>
        List<Tuple<InstallationInfo, CancellationTokenSource>> CurrentInstallations { get; set; }

        /// <summary>
        /// The completed installations
        /// </summary>
        ConcurrentBag<InstallationInfo> CompletedInstallations { get; set; }

        /// <summary>
        /// Occurs when [plugin uninstalled].
        /// </summary>
        event EventHandler<GenericEventArgs<IPlugin>> PluginUninstalled;

        /// <summary>
        /// Occurs when [plugin updated].
        /// </summary>
        event EventHandler<GenericEventArgs<Tuple<IPlugin, PackageVersionInfo>>> PluginUpdated;

        /// <summary>
        /// Occurs when [plugin updated].
        /// </summary>
        event EventHandler<GenericEventArgs<PackageVersionInfo>> PluginInstalled;

        /// <summary>
        /// Gets all available packages.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="withRegistration">if set to <c>true</c> [with registration].</param>
        /// <param name="packageType">Type of the package.</param>
        /// <param name="applicationVersion">The application version.</param>
        /// <returns>Task{List{PackageInfo}}.</returns>
        Task<IEnumerable<PackageInfo>> GetAvailablePackages(CancellationToken cancellationToken,
            bool withRegistration = true,
                                                                                  string packageType = null,
                                                                                  Version applicationVersion = null);

        /// <summary>
        /// Gets all available packages from a static resource.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{List{PackageInfo}}.</returns>
        Task<IEnumerable<PackageInfo>> GetAvailablePackagesWithoutRegistrationInfo(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the package.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="guid">The assembly guid</param>
        /// <param name="classification">The classification.</param>
        /// <param name="version">The version.</param>
        /// <returns>Task{PackageVersionInfo}.</returns>
        Task<PackageVersionInfo> GetPackage(string name, string guid, PackageVersionClass classification, Version version);

        /// <summary>
        /// Gets the latest compatible version.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="guid">The assembly guid</param>
        /// <param name="currentServerVersion">The current server version.</param>
        /// <param name="classification">The classification.</param>
        /// <returns>Task{PackageVersionInfo}.</returns>
        Task<PackageVersionInfo> GetLatestCompatibleVersion(string name, string guid, Version currentServerVersion, PackageVersionClass classification = PackageVersionClass.Release);

        /// <summary>
        /// Gets the latest compatible version.
        /// </summary>
        /// <param name="availablePackages">The available packages.</param>
        /// <param name="name">The name.</param>
        /// <param name="guid">The assembly guid</param>
        /// <param name="currentServerVersion">The current server version.</param>
        /// <param name="classification">The classification.</param>
        /// <returns>PackageVersionInfo.</returns>
        PackageVersionInfo GetLatestCompatibleVersion(IEnumerable<PackageInfo> availablePackages, string name, string guid, Version currentServerVersion, PackageVersionClass classification = PackageVersionClass.Release);

        /// <summary>
        /// Gets the available plugin updates.
        /// </summary>
        /// <param name="applicationVersion">The current server version.</param>
        /// <param name="withAutoUpdateEnabled">if set to <c>true</c> [with auto update enabled].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{PackageVersionInfo}}.</returns>
        Task<IEnumerable<PackageVersionInfo>> GetAvailablePluginUpdates(Version applicationVersion, bool withAutoUpdateEnabled, CancellationToken cancellationToken);

        /// <summary>
        /// Installs the package.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="isPlugin">if set to <c>true</c> [is plugin].</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">package</exception>
        Task InstallPackage(PackageVersionInfo package, bool isPlugin, IProgress<double> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Uninstalls a plugin
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        /// <exception cref="System.ArgumentException"></exception>
        void UninstallPlugin(IPlugin plugin);
    }
}