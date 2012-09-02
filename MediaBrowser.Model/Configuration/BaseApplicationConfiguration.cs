
namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Serves as a common base class for the Server and UI application Configurations
    /// </summary>
    public class BaseApplicationConfiguration
    {
        public bool EnableDebugLevelLogging { get; set; }
        public int HttpServerPortNumber { get; set; }

        public BaseApplicationConfiguration()
        {
            HttpServerPortNumber = 8096;
        }
    }
}
