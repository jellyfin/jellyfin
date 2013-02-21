using MediaBrowser.Model.Configuration;

namespace MediaBrowser.UI.Configuration
{
    /// <summary>
    /// This is the UI's device configuration that applies regardless of which user is logged in.
    /// </summary>
    public class UIApplicationConfiguration : BaseApplicationConfiguration
    {
        /// <summary>
        /// Gets or sets the server host name (myserver or 192.168.x.x)
        /// </summary>
        public string ServerHostName { get; set; }

        /// <summary>
        /// Gets or sets the port number used by the API
        /// </summary>
        public int ServerApiPort { get; set; }

        public UIApplicationConfiguration()
            : base()
        {
            ServerHostName = "localhost";
            ServerApiPort = 8096;
        }
    }
}
