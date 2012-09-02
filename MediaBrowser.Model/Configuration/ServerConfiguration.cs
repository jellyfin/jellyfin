
namespace MediaBrowser.Model.Configuration
{
    public class ServerConfiguration : BaseApplicationConfiguration
    {
        public bool EnableInternetProviders { get; set; }
        public string WeatherZipCode { get; set; }
    }
}
