using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Events;
using System;

namespace MediaBrowser.Controller.Configuration
{
    /// <summary>
    /// Interface IServerConfigurationManager
    /// </summary>
    public interface IServerConfigurationManager : IConfigurationManager
    {
        /// <summary>
        /// Occurs when [configuration updating].
        /// </summary>
        event EventHandler<GenericEventArgs<ServerConfiguration>> ConfigurationUpdating;
        
        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        IServerApplicationPaths ApplicationPaths { get; }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        ServerConfiguration Configuration { get; }

        /// <summary>
        /// Sets the preferred metadata service.
        /// </summary>
        /// <param name="service">The service.</param>
        void SetPreferredMetadataService(string service);
    }
}
