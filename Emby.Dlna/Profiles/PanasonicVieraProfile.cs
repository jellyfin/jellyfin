using MediaBrowser.Model.Dlna;
using System.Xml.Serialization;

namespace Emby.Dlna.Profiles
{
    [XmlRoot("Profile")]
    public class PanasonicVieraProfile : DefaultProfile
    {
        public PanasonicVieraProfile()
        {
            Name = "Panasonic Viera";

            Identification = new DeviceIdentification
            {
                FriendlyName = @"VIERA",
                Manufacturer = "Panasonic",

                Headers = new[]
               {
                   new HttpHeaderInfo
                   {
                       Name = "User-Agent",
                       Value = "Panasonic MIL DLNA",
                       Match = HeaderMatchType.Substring
                   }
               }
            };

            AddXmlRootAttribute("xmlns:pv", "http://www.pv.com/pvns/");

            TimelineOffsetSeconds = 10;

            ContainerProfiles = new[]
            {
                new ContainerProfile
                {
                    Type = DlnaProfileType.Photo,

                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Width,
                            Value = "1920"
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Height,
                            Value = "1080"
                        }
                    }
                }
            };

            CodecProfiles = new[]
           {
               new CodecProfile
               {
                   Type = CodecType.Video,

                   Conditions = new[]
                   {
                       new ProfileCondition
                       {
                           Condition = ProfileConditionType.LessThanEqual,
                           Property = ProfileConditionValue.Width,
                           Value = "1920"
                       },
                       new ProfileCondition
                       {
                           Condition = ProfileConditionType.LessThanEqual,
                           Property = ProfileConditionValue.Height,
                           Value = "1080"
                       },
                       new ProfileCondition
                       {
                           Condition = ProfileConditionType.LessThanEqual,
                           Property = ProfileConditionValue.VideoBitDepth,
                           Value = "8",
                           IsRequired = false
                       }
                   }
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
                    Type = DlnaProfileType.Video,
                    Container = "ts",
                    OrgPn = "MPEG_TS_SD_EU,MPEG_TS_SD_NA,MPEG_TS_SD_KO",
                    MimeType = "video/vnd.dlna.mpeg-tts"
                },
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
