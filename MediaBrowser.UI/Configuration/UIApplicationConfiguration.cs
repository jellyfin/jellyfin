using MediaBrowser.Model.Configuration;
using System.Windows;

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
        /// <value>The name of the server host.</value>
        public string ServerHostName { get; set; }

        /// <summary>
        /// Gets or sets the port number used by the API
        /// </summary>
        /// <value>The server API port.</value>
        public int ServerApiPort { get; set; }

        /// <summary>
        /// Gets or sets the player configurations.
        /// </summary>
        /// <value>The player configurations.</value>
        public PlayerConfiguration[] MediaPlayers { get; set; }

        /// <summary>
        /// Gets or sets the state of the window.
        /// </summary>
        /// <value>The state of the window.</value>
        public WindowState? WindowState { get; set; }

        /// <summary>
        /// Gets or sets the window top.
        /// </summary>
        /// <value>The window top.</value>
        public double? WindowTop { get; set; }

        /// <summary>
        /// Gets or sets the window left.
        /// </summary>
        /// <value>The window left.</value>
        public double? WindowLeft { get; set; }

        /// <summary>
        /// Gets or sets the width of the window.
        /// </summary>
        /// <value>The width of the window.</value>
        public double? WindowWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the window.
        /// </summary>
        /// <value>The height of the window.</value>
        public double? WindowHeight { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UIApplicationConfiguration" /> class.
        /// </summary>
        public UIApplicationConfiguration()
            : base()
        {
            ServerHostName = "localhost";
            ServerApiPort = 8096;

            // Need a different default than the server
            LegacyWebSocketPortNumber = 8946;
        }
    }
}
