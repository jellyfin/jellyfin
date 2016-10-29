using MediaBrowser.Model.Dlna;
using System.Xml.Serialization;

namespace MediaBrowser.Dlna.Profiles
{
    [XmlRoot("Profile")]
    public class DenonAvrProfile : DefaultProfile
    {
        public DenonAvrProfile()
        {
            Name = "Denon AVR";

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
