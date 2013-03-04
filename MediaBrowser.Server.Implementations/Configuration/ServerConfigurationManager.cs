using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Implementations.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;

namespace MediaBrowser.Server.Implementations.Configuration
{
    /// <summary>
    /// Class ServerConfigurationManager
    /// </summary>
    public class ServerConfigurationManager : BaseConfigurationManager, IServerConfigurationManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConfigurationManager" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        public ServerConfigurationManager(IApplicationPaths applicationPaths, ILogManager logManager, IXmlSerializer xmlSerializer)
            : base(applicationPaths, logManager, xmlSerializer)
        {
        }

        /// <summary>
        /// Gets the type of the configuration.
        /// </summary>
        /// <value>The type of the configuration.</value>
        protected override Type ConfigurationType
        {
            get { return typeof(ServerConfiguration); }
        }

        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        public IServerApplicationPaths ApplicationPaths
        {
            get { return (IServerApplicationPaths)CommonApplicationPaths; }
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public ServerConfiguration Configuration
        {
            get { return (ServerConfiguration)CommonConfiguration; }
        }
    }
}
