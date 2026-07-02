using System;
using System.IO;
using Emby.Server.Implementations.Serialization;
using MediaBrowser.Model.Dlna;
using Xunit;

namespace Jellyfin.Model.Tests.Dlna;

public class SerializationTests
{
    [Fact]
    public void DeviceProfile_XmlSerialization()
    {
        var serializer = new MyXmlSerializer();
        var original = new DeviceProfile
        {
            Name = "Test Device",
            Id = Guid.NewGuid(),
            MaxStreamingBitrate = 9500000,
            MaxStaticBitrate = 8500000,
            MusicStreamingTranscodingBitrate = 192000,
            MaxStaticMusicBitrate = 5000000,
            DirectPlayProfiles =
            [
                new DirectPlayProfile
                {
                    Container = "mp4,mkv",
                    AudioCodec = "aac,ac3",
                    VideoCodec = "h264,hevc",
                    Type = DlnaProfileType.Video
                }
            ],
            TranscodingProfiles =
            [
                new TranscodingProfile
                {
                    Container = "ts",
                    Type = DlnaProfileType.Video,
                    VideoCodec = "h264",
                    AudioCodec = "aac",
                    SegmentLength = 6,
                    Conditions =
                    [
                        new ProfileCondition(ProfileConditionType.Equals, ProfileConditionValue.VideoBitDepth, "8")
                    ]
                }
            ],
            ContainerProfiles =
            [
                new ContainerProfile
                {
                    Type = DlnaProfileType.Video,
                    Container = "mkv",
                    SubContainer = "matroska",
                    Conditions =
                    [
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920")
                    ]
                }
            ],
            CodecProfiles =
            [
                new CodecProfile
                {
                    Type = CodecType.Video,
                    Codec = "h264",
                    Container = "mp4",
                    Conditions =
                    [
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoLevel, "41")
                    ],
                    ApplyConditions =
                    [
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitrate, "10000000")
                    ]
                }
            ],
            SubtitleProfiles =
            [
                new SubtitleProfile
                {
                    Format = "srt",
                    Method = SubtitleDeliveryMethod.Embed,
                    Language = "eng",
                    Container = "mp4"
                }
            ]
        };

        using var stream = new MemoryStream();
        serializer.SerializeToStream(original, stream);
        stream.Position = 0;

        var deserialized = Assert.IsType<DeviceProfile>(serializer.DeserializeFromStream(typeof(DeviceProfile), stream));

        Assert.Equivalent(original, deserialized, strict: false);
    }
}
