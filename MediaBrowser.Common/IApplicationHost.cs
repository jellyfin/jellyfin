using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Updates;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common
{
    /// <summary>
    /// An interface to be implemented by the applications hosting a kernel
    /// </summary>
    public interface IApplicationHost
    {
        /// <summary>
        /// Gets the display name of the operating system.
        /// </summary>
        /// <value>The display name of the operating system.</value>
        string OperatingSystemDisplayName { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the device identifier.
        /// </summary>
        /// <value>The device identifier.</value>
        string SystemId { get; }

        /// <summary>
        /// Occurs when [application updated].
        /// </summary>
        event EventHandler<GenericEventArgs<PackageVersionInfo>> ApplicationUpdated;

        /// <summary>
        /// Gets a value indicating whether this instance is running as service.
        /// </summary>
        /// <value><c>true</c> if this instance is running as service; otherwise, <c>false</c>.</value>
        bool IsRunningAsService { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has pending kernel reload.
        /// </summary>
        /// <value><c>true</c> if this instance has pending kernel reload; otherwise, <c>false</c>.</value>
        bool HasPendingRestart { get; }

        /// <summary>
        /// Gets a value indicating whether this instance can self restart.
        /// </summary>
        /// <value><c>true</c> if this instance can self restart; otherwise, <c>false</c>.</value>
        bool CanSelfRestart { get; }

        /// <summary>
        /// Occurs when [has pending restart changed].
        /// </summary>
        event EventHandler HasPendingRestartChanged;

        /// <summary>
        /// Notifies the pending restart.
        /// </summary>
        void NotifyPendingRestart();
        
        /// <summary>
        /// Restarts this instance.
        /// </summary>
        Task Restart();

        /// <summary>
        /// Gets the application version.
        /// </summary>
        /// <value>The application version.</value>
        Version ApplicationVersion { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can self update.
        /// </summary>
        /// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
        bool CanSelfUpdate { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is first run.
        /// </summary>
        /// <value><c>true</c> if this instance is first run; otherwise, <c>false</c>.</value>
        bool IsFirstRun { get; }

        /// <summary>
        /// Gets the failed assemblies.
        /// </summary>
        /// <value>The failed assemblies.</value>
        List<string> FailedAssemblies { get; }

        /// <summary>
        /// Gets all concrete types.
        /// </summary>
        /// <value>All concrete types.</value>
        Type[] AllConcreteTypes { get; }

        /// <summary>
        /// Gets the exports.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="manageLiftime">if set to <c>true</c> [manage liftime].</param>
        /// <returns>IEnumerable{``0}.</returns>
        IEnumerable<T> GetExports<T>(bool manageLiftime = true);

        /// <summary>
        /// Checks for update.
        /// </summary>
        /// <returns>Task{CheckForUpdateResult}.</returns>
        Task<CheckForUpdateResult> CheckForApplicationUpdate(CancellationToken cancellationToken, IProgress<double> progress);

        /// <summary>
        /// Updates the application.
        /// </summary>
        /// <returns>Task.</returns>
        Task UpdateApplication(PackageVersionInfo package, CancellationToken cancellationToken, IProgress<double> progress);

        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>``0.</returns>
        T Resolve<T>();

        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>``0.</returns>
        T TryResolve<T>();

        /// <summary>
        /// Shuts down.
        /// </summary>
        Task Shutdown();

        /// <summary>
        /// Gets the plugins.
        /// </summary>
        /// <value>The plugins.</value>
        IPlugin[] Plugins { get; }

        /// <summary>
        /// Removes the plugin.
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        void RemovePlugin(IPlugin plugin);

        /// <summary>
        /// Inits this instance.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        Task Init(IProgress<double> progress);

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        object CreateInstance(Type type);
    }
}
