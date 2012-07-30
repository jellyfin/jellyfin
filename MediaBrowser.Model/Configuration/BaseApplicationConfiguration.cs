using MediaBrowser.Logging;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Serves as a common base class for the Server and UI application Configurations
    /// </summary>
    public class BaseApplicationConfiguration
    {
        public LogSeverity LogSeverity { get; set; }
        public int HttpServerPortNumber { get; set; }

        public BaseApplicationConfiguration()
        {
            LogSeverity = LogSeverity.Info;
            HttpServerPortNumber = 8096;
        }
    }
}
