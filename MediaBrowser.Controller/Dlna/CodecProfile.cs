using System.Collections.Generic;

namespace MediaBrowser.Controller.Dlna
{
    public class CodecProfile
    {
        public CodecType Type { get; set; }
        public List<ProfileCondition> Conditions { get; set; }
        public string[] Codecs { get; set; }

        public CodecProfile()
        {
            Conditions = new List<ProfileCondition>();
            Codecs = new string[] { };
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
        Filesize,
        Width,
        Height,
        VideoBitrate,
        VideoFramerate,
        VideoLevel
    }
}
