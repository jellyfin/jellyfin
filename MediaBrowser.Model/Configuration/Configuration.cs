using MediaBrowser.Model.Logging;

namespace MediaBrowser.Model.Configuration
{
    public class Configuration
    {
        public string ImagesByNamePath { get; set; }
        public int HttpServerPortNumber { get; set; }
        public int RecentItemDays { get; set; }
        public LogSeverity LogSeverity { get; set; }

        public Configuration()
        {
            HttpServerPortNumber = 8096;
            RecentItemDays = 14;
            LogSeverity = LogSeverity.Info;
        }
    }
}
