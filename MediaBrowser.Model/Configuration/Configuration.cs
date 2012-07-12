using MediaBrowser.Common.Logging;

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
            LogSeverity = Common.Logging.LogSeverity.Info;
        }
    }
}
