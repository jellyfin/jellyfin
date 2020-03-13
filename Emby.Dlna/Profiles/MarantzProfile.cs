#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class MarantzProfile : DefaultProfile
    {
        public MarantzProfile()
        {
            Name = "Marantz";

            SupportedMediaTypes = "Audio";

            Identification = new DeviceIdentification
            {
                Manufacturer = @"Marantz",

                Headers = new[]
               {
                   new HttpHeaderInfo
                   {
                       Name = "User-Agent",
                       Value = "Marantz",
                       Match = HeaderMatchType.Substring
                   }
               }
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "aac,mp3,wav,wma,flac",
                    Type = DlnaProfileType.Audio
                },
            };

            ResponseProfiles = new ResponseProfile[] { };
        }
    }
}
