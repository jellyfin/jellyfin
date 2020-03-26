#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class Foobar2000Profile : DefaultProfile
    {
        public Foobar2000Profile()
        {
            Name = "foobar2000";

            SupportedMediaTypes = "Audio";

            Identification = new DeviceIdentification
            {
                FriendlyName = @"foobar",

                Headers = new[]
               {
                   new HttpHeaderInfo
                   {
                       Name = "User-Agent",
                       Value = "foobar",
                       Match = HeaderMatchType.Substring
                   }
               }
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "mp3",
                    AudioCodec = "mp2,mp3",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Container = "mp4",
                    AudioCodec = "mp4",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Container = "aac,wav",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Container = "flac",
                    AudioCodec = "flac",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Container = "asf",
                    AudioCodec = "wmav2,wmapro,wmavoice",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Container = "ogg",
                    AudioCodec = "vorbis",
                    Type = DlnaProfileType.Audio
                }
            };

            ResponseProfiles = new ResponseProfile[] { };
        }
    }
}
