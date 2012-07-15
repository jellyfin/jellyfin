using MediaBrowser.Model.Logging;

namespace MediaBrowser.Model.Configuration
{
    public class Configuration
    {
        public string ImagesByNamePath { get; set; }
        public int HttpServerPortNumber { get; set; }
        public LogSeverity LogSeverity { get; set; }

        public Configuration()
        {
            HttpServerPortNumber = 8096;
            LogSeverity = LogSeverity.Info;
        }
    }
}
