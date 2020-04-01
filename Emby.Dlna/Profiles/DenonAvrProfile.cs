#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class DenonAvrProfile : DefaultProfile
    {
        public DenonAvrProfile()
        {
            Name = "Denon AVR";

            SupportedMediaTypes = "Audio";

            Identification = new DeviceIdentification
            {
                FriendlyName = @"Denon:\[AVR:.*",
                Manufacturer = "Denon"
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "mp3,flac,m4a,wma",
                    Type = DlnaProfileType.Audio
                },
            };

            ResponseProfiles = new ResponseProfile[] { };
        }
    }
}
