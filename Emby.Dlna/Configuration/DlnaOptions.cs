#pragma warning disable CS1591

namespace Emby.Dlna.Configuration
{
    public class DlnaOptions
    {
        public DlnaOptions()
        {
            EnablePlayTo = true;
            EnableServer = true;
            BlastAliveMessages = true;
            SendOnlyMatchedHost = true;
            ClientDiscoveryIntervalSeconds = 60;
            BlastAliveMessageIntervalSeconds = 1800;
        }

        public bool EnablePlayTo { get; set; }

        public bool EnableServer { get; set; }

        public bool EnableDebugLog { get; set; }

        public bool BlastAliveMessages { get; set; }

        public bool SendOnlyMatchedHost { get; set; }

        public int ClientDiscoveryIntervalSeconds { get; set; }

        public int BlastAliveMessageIntervalSeconds { get; set; }

        public string DefaultUserId { get; set; }
    }
}
