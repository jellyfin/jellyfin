#pragma warning disable CS1591

using MediaBrowser.Model.QuickConnect;

namespace Emby.Server.Implementations.QuickConnect
{
    public class QuickConnectConfiguration
    {
        public QuickConnectConfiguration()
        {
        }

        public QuickConnectState State { get; set; }
    }
}
