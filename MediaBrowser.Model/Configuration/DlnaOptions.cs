
namespace MediaBrowser.Model.Configuration
{
    public class DlnaOptions
    {
        public bool EnablePlayTo { get; set; }
        public bool EnableServer { get; set; }
        public bool EnableDebugLogging { get; set; }
        public int ClientDiscoveryIntervalSeconds { get; set; }

        public DlnaOptions()
        {
            EnablePlayTo = true;
            EnableServer = true;
            ClientDiscoveryIntervalSeconds = 60;
        }
    }
}
