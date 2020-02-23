#pragma warning disable CS1591

using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.Profiles
{
    [System.Xml.Serialization.XmlRoot("Profile")]
    public class SharpSmartTvProfile : DefaultProfile
    {
        public SharpSmartTvProfile()
        {
            Name = "Sharp Smart TV";

            RequiresPlainFolders = true;
            RequiresPlainVideoItems = true;

            Identification = new DeviceIdentification
            {
                Manufacturer = "Sharp",

                Headers = new[]
               {
                   new HttpHeaderInfo
                   {
                       Name = "User-Agent",
                       Value = "Sharp",
                       Match = HeaderMatchType.Substring
                   }
               }
            };

            TranscodingProfiles = new[]
            {
                new TranscodingProfile
                {
                    Container = "mp3",
                    AudioCodec = "mp3",
                    Type = DlnaProfileType.Audio
                },

                new TranscodingProfile
                {
                    Container = "ts",
                    Type = DlnaProfileType.Video,
                    AudioCodec = "ac3,aac,mp3,dts,dca",
                    VideoCodec = "h264",
                    EnableMpegtsM2TsMode = true
                },

                new TranscodingProfile
                {
                    Container = "jpeg",
                    Type = DlnaProfileType.Photo
                }
            };

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "m4v,mkv,avi,mov,mp4",
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = "aac,mp3,ac3,dts,dca",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "asf,wmv",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "mpg,mpeg",
                    VideoCodec = "mpeg2video",
                    AudioCodec = "mp3,aac",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "flv",
                    VideoCodec = "h264",
                    AudioCodec = "mp3,aac",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "mp3,wav",
                    Type = DlnaProfileType.Audio
                }
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile
                {
                    Format = "srt",
                    Method = SubtitleDeliveryMethod.Embed
                },
                new SubtitleProfile
                {
                    Format = "srt",
                    Method = SubtitleDeliveryMethod.External
                }
            };

            ResponseProfiles = new[]
            {
                new ResponseProfile
                {
                    Container = "m4v",
                    Type = DlnaProfileType.Video,
                    MimeType = "video/mp4"
                }
            };
        }
    }
}
