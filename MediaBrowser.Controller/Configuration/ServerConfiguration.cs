using MediaBrowser.Common.Configuration;

namespace MediaBrowser.Controller.Configuration
{
    public class ServerConfiguration : BaseApplicationConfiguration
    {
        public bool EnableInternetProviders { get; set; }
    }
}
