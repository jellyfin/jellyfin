using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace MediaBrowser.Common
{
    /// <summary>
    /// Delegate used with GetExports{T}.
    /// </summary>
    /// <param name="type">Type to create.</param>
    /// <returns>New instance of type <param>type</param>.</returns>
    public delegate object? CreationDelegateFactory(Type type);

    /// <summary>
    /// An interface to be implemented by the applications hosting a kernel.
    /// </summary>
    public interface IApplicationHost
    {
        /// <summary>
        /// Occurs when [has pending restart changed].
        /// </summary>
        event EventHandler? HasPendingRestartChanged;

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
        /// Gets a value indicating whether this instance has pending changes requiring a restart.
        /// </summary>
        /// <value><c>true</c> if this instance has a pending restart; otherwise, <c>false</c>.</value>
        bool HasPendingRestart { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the application should restart.
        /// </summary>
        bool ShouldRestart { get; set; }

        /// <summary>
        /// Gets the application version.
        /// </summary>
        /// <value>The application version.</value>
        Version ApplicationVersion { get; }

        /// <summary>
        /// Gets or sets the service provider.
        /// </summary>
        IServiceProvider? ServiceProvider { get; set; }

        /// <summary>
        /// Gets the application version.
        /// </summary>
        /// <value>The application version.</value>
        string ApplicationVersionString { get; }

        /// <summary>
        /// Gets the application user agent.
        /// </summary>
        /// <value>The application user agent.</value>
        string ApplicationUserAgent { get; }

        /// <summary>
        /// Gets the email address for use within a comment section of a user agent field.
        /// Presently used to provide contact information to MusicBrainz service.
        /// </summary>
        string ApplicationUserAgentAddress { get; }

        /// <summary>
        /// Gets all plugin assemblies which implement a custom rest api.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{Assembly}"/> containing the plugin assemblies.</returns>
        IEnumerable<Assembly> GetApiPluginAssemblies();

        /// <summary>
        /// Notifies the pending restart.
        /// </summary>
        void NotifyPendingRestart();

        /// <summary>
        /// Gets the exports.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="manageLifetime">If set to <c>true</c> [manage lifetime].</param>
        /// <returns><see cref="IReadOnlyCollection{T}" />.</returns>
        IReadOnlyCollection<T> GetExports<T>(bool manageLifetime = true);

        /// <summary>
        /// Gets the exports.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="defaultFunc">Delegate function that gets called to create the object.</param>
        /// <param name="manageLifetime">If set to <c>true</c> [manage lifetime].</param>
        /// <returns><see cref="IReadOnlyCollection{T}" />.</returns>
        IReadOnlyCollection<T> GetExports<T>(CreationDelegateFactory defaultFunc, bool manageLifetime = true);

        /// <summary>
        /// Gets the export types.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>IEnumerable{Type}.</returns>
        IEnumerable<Type> GetExportTypes<T>();

        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="T">The <c>Type</c>.</typeparam>
        /// <returns>``0.</returns>
        T Resolve<T>();

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="serviceCollection">Instance of the <see cref="IServiceCollection"/> interface.</param>
        void Init(IServiceCollection serviceCollection);
    }
}
