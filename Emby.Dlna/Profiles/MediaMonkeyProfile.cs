#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class MediaMonkeyProfile : DefaultProfile
    {
        public MediaMonkeyProfile()
        {
            Name = "MediaMonkey";

            SupportedMediaTypes = "Audio";

            Identification = new DeviceIdentification
            {
                FriendlyName = @"MediaMonkey",

                Headers = new[]
               {
                   new HttpHeaderInfo
                   {
                       Name = "User-Agent",
                       Value = "MediaMonkey",
                       Match = HeaderMatchType.Substring
                   }
               }
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "aac,mp3,mpa,wav,wma,mp2,ogg,oga,webma,ape,opus,flac,m4a",
                    Type = DlnaProfileType.Audio
                }
            };

            ResponseProfiles = new ResponseProfile[] { };
        }
    }
}
