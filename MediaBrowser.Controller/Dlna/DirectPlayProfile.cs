using System.Collections.Generic;

namespace MediaBrowser.Controller.Dlna
{
    public class DirectPlayProfile
    {
        public string[] Containers { get; set; }
        public string[] AudioCodecs { get; set; }
        public string[] VideoCodecs { get; set; }

        public DlnaProfileType Type { get; set; }

        public List<ProfileCondition> Conditions { get; set; }

        public DirectPlayProfile()
        {
            Conditions = new List<ProfileCondition>();

            AudioCodecs = new string[] { };
            VideoCodecs = new string[] { };
            Containers = new string[] { };
        }
    }

    public class ProfileCondition
    {
        public ProfileConditionType Condition { get; set; }
        public ProfileConditionValue Property { get; set; }
        public string Value { get; set; }
    }

    public enum DlnaProfileType
    {
        Audio = 0,
        Video = 1,
        Photo = 2
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
        VideoWidth,
        VideoHeight,
        VideoBitrate,
        VideoFramerate,
        VideoLevel
    }
}
