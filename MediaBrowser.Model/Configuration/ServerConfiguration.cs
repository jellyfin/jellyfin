using MediaBrowser.Model.Weather;
using ProtoBuf;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Represents the server configuration.
    /// </summary>
    [ProtoContract]
    public class ServerConfiguration : BaseApplicationConfiguration
    {
        [ProtoMember(3)]
        public bool EnableInternetProviders { get; set; }

        [ProtoMember(4)]
        public bool EnableUserProfiles { get; set; }

        [ProtoMember(5)]
        public string WeatherZipCode { get; set; }

        [ProtoMember(6)]
        public WeatherUnits WeatherUnit { get; set; }

        public ServerConfiguration()
            : base()
        {
            EnableUserProfiles = true;
        }
    }
}
