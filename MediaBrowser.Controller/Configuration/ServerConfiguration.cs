using MediaBrowser.Common.Configuration;

namespace MediaBrowser.Controller.Configuration
{
    public class ServerConfiguration : BaseConfiguration
    {
        public string ImagesByNamePath { get; set; }
        public int RecentItemDays { get; set; }

        public ServerConfiguration()
            : base()
        {
            RecentItemDays = 14;
        }
    }
}
