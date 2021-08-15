using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.Configuration
{
    /// <summary>
    /// Interface IServerConfigurationManager.
    /// </summary>
    public interface IServerConfigurationManager : IConfigurationManager
    {
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
