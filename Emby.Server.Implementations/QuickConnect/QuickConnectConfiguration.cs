using MediaBrowser.Model.QuickConnect;

namespace Emby.Server.Implementations.QuickConnect
{
    /// <summary>
    /// Persistent quick connect configuration.
    /// </summary>
    public class QuickConnectConfiguration
    {
        /// <summary>
        /// Gets or sets persistent quick connect availability state.
        /// </summary>
        public QuickConnectState State { get; set; }
    }
}
