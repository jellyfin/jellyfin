using MediaBrowser.Model.Updates;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// An interface to be implemented by the applications hosting a kernel
    /// </summary>
    public interface IApplicationHost
    {
        /// <summary>
        /// Restarts this instance.
        /// </summary>
        void Restart();

        /// <summary>
        /// Reloads the logger.
        /// </summary>
        void ReloadLogger();

        /// <summary>
        /// Gets the log file path.
        /// </summary>
        /// <value>The log file path.</value>
        string LogFilePath { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can self update.
        /// </summary>
        /// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
        bool CanSelfUpdate { get; }

        /// <summary>
        /// Checks for update.
        /// </summary>
        /// <returns>Task{CheckForUpdateResult}.</returns>
        Task<CheckForUpdateResult> CheckForApplicationUpdate(CancellationToken cancellationToken, IProgress<double> progress);

        /// <summary>
        /// Updates the application.
        /// </summary>
        /// <returns>Task.</returns>
        Task UpdateApplication(CancellationToken cancellationToken, IProgress<double> progress);

        /// <summary>
        /// Creates an instance of type and resolves all constructor dependancies
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        object CreateInstance(Type type);

        /// <summary>
        /// Registers a service that other classes can use as a dependancy.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The obj.</param>
        void Register<T>(T obj) where T : class;

        /// <summary>
        /// Registers the specified service type.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="implementation">Type of the implementation.</param>
        void Register(Type serviceType, Type implementation);

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
    }
}
