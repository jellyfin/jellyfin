
namespace MediaBrowser.Model.Configuration
{
    public class DlnaOptions
    {
        public bool EnablePlayTo { get; set; }
        public bool EnableServer { get; set; }
        public bool EnableDebugLog { get; set; }
        public bool BlastAliveMessages { get; set; }
        public int ClientDiscoveryIntervalSeconds { get; set; }
        public int BlastAliveMessageIntervalSeconds { get; set; }
        public string DefaultUserId { get; set; }
        public bool EnableMovieFolders { get; set; }

        public DlnaOptions()
        {
            EnablePlayTo = true;
            EnableServer = true;
            BlastAliveMessages = true;
            ClientDiscoveryIntervalSeconds = 60;
            BlastAliveMessageIntervalSeconds = 30;
        }
    }
}
