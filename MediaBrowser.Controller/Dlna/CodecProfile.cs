using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Dlna
{
    public class CodecProfile
    {
        public CodecType Type { get; set; }
        public ProfileCondition[] Conditions { get; set; }
        public string Codec { get; set; }

        public CodecProfile()
        {
            Conditions = new ProfileCondition[] {};
        }

        public List<string> GetCodecs()
        {
            return (Codec ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
        }
    }

    public enum CodecType
    {
        VideoCodec = 0,
        VideoAudioCodec = 1,
        AudioCodec = 2
    }

    public class ProfileCondition
    {
        public ProfileConditionType Condition { get; set; }
        public ProfileConditionValue Property { get; set; }
        public string Value { get; set; }
        public bool IsRequired { get; set; }

        public ProfileCondition()
        {
            IsRequired = true;
        }
    }

    public enum ProfileConditionType
    {
        Equals = 0,
        NotEquals = 1,
        LessThanEqual = 2,
        GreaterThanEqual = 3
    }

    public enum ProfileConditionValue
    {
        AudioChannels,
        AudioBitrate,
        AudioProfile,
        Filesize,
        Width,
        Height,
        Has64BitOffsets,
        VideoBitDepth,
        VideoBitrate,
        VideoFramerate,
        VideoLevel,
        VideoProfile
    }
}
