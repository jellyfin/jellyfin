using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Model.Configuration;
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
    }
}
