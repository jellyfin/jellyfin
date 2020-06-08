using MediaBrowser.Model.QuickConnect;

namespace Emby.Server.Implementations.QuickConnect
{
    /// <summary>
    /// Persistent quick connect configuration
    /// </summary>
    public class QuickConnectConfiguration
    {
        /// <summary>
        /// Quick connect configuration object
        /// </summary>
        public QuickConnectConfiguration()
        {
        }

        /// <summary>
        /// Persistent quick connect availability state
        /// </summary>
        public QuickConnectState State { get; set; }
    }
}
