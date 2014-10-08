using System.Collections.Generic;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna.Profiles
{
    [XmlRoot("Profile")]
    public class AndroidProfile : DefaultProfile
    {
        public AndroidProfile(bool supportsHls, bool supportsMpegDash)
        {
            Name = "Android";

            List<TranscodingProfile> transcodingProfiles = new List<TranscodingProfile>();

            transcodingProfiles.Add(new TranscodingProfile
            {
                Container = "mp3",
                AudioCodec = "mp3",
                Type = DlnaProfileType.Audio
            });

            if (supportsMpegDash)
            {

            }
            if (supportsHls)
            {
                transcodingProfiles.Add(new TranscodingProfile
                {
                    Protocol = "hls",
                    Container = "ts",
                    VideoCodec = "h264",
                    AudioCodec = "aac",
                    Type = DlnaProfileType.Video,
                    VideoProfile = "Baseline",
                    Context = EncodingContext.Streaming
                });
            }
            transcodingProfiles.Add(new TranscodingProfile
            {
                Container = "mp4",
                VideoCodec = "h264",
                AudioCodec = "aac",
                Type = DlnaProfileType.Video,
                VideoProfile = "Baseline",
                Context = EncodingContext.Static
            });

            TranscodingProfiles = transcodingProfiles.ToArray();

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "mp4",
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = "aac",
                    Type = DlnaProfileType.Video
                },

                new DirectPlayProfile
                {
                    Container = "mp4,aac",
                    AudioCodec = "aac",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Container = "mp3",
                    AudioCodec = "mp3",
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
                    Container = "ogg",
                    AudioCodec = "vorbis",
                    Type = DlnaProfileType.Audio
                },

                new DirectPlayProfile
                {
                    Container = "jpeg,png,gif,bmp",
                    Type = DlnaProfileType.Photo
                }
            };

            CodecProfiles = new[]
            {
                new CodecProfile
                {
                    Type = CodecType.Video,
                    Codec= "h264",

                    Conditions = new []
                    {
                        new ProfileCondition(ProfileConditionType.SubstringOf, ProfileConditionValue.VideoProfile, "baseline"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitDepth, "8"),
                        new ProfileCondition(ProfileConditionType.NotEquals, ProfileConditionValue.IsAnamorphic, "true")
                    }
                },

                new CodecProfile
                {
                    Type = CodecType.Video,

                    Conditions = new []
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Width, "1920"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.Height, "1080"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.VideoBitDepth, "8"),
                        new ProfileCondition(ProfileConditionType.NotEquals, ProfileConditionValue.IsAnamorphic, "true")
                    }
                },

                new CodecProfile
                {
                    Type = CodecType.VideoAudio,
                    Codec = "aac",
                    Conditions = new []
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "2")
                    }
                },

                new CodecProfile
                {
                    Type = CodecType.Audio,
                    Codec = "aac",
                    Conditions = new []
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "2")
                    }
                },

                new CodecProfile
                {
                    Type = CodecType.Audio,
                    Codec = "mp3",
                    Conditions = new []
                    {
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioChannels, "2"),
                        new ProfileCondition(ProfileConditionType.LessThanEqual, ProfileConditionValue.AudioBitrate, "320000")
                    }
                }
            };

        }
    }
}
