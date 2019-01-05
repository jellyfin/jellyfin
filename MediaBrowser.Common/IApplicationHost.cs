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
        /// Gets or sets a value indicating whether this instance has pending kernel reload.
        /// </summary>
        /// <value><c>true</c> if this instance has pending kernel reload; otherwise, <c>false</c>.</value>
        bool HasPendingRestart { get; }

        bool IsShuttingDown { get; }

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
        void Restart();

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
        /// Gets the exports.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="manageLiftime">if set to <c>true</c> [manage liftime].</param>
        /// <returns>IEnumerable{``0}.</returns>
        IEnumerable<T> GetExports<T>(bool manageLiftime = true);

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
        void Init();

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        object CreateInstance(Type type);

        PackageVersionClass SystemUpdateLevel { get; }
    }
}
