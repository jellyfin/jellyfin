using MediaBrowser.Common.Logging;

namespace MediaBrowser.Common.Configuration
{
    /// <summary>
    /// Serves as a common base class for the Server and UI application Configurations
    /// </summary>
    public class BaseConfiguration
    {
        public LogSeverity LogSeverity { get; set; }
        public int HttpServerPortNumber { get; set; }

        public BaseConfiguration()
        {
            LogSeverity = LogSeverity.Info;
            HttpServerPortNumber = 8096;
        }
    }
}
