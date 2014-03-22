using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace MediaBrowser.Controller.Dlna
{
    public class DirectPlayProfile
    {
        public string Container { get; set; }
        public string AudioCodec { get; set; }
        public string VideoCodec { get; set; }

        [IgnoreDataMember]
        [XmlIgnore]
        public string[] Containers
        {
            get
            {
                return (Container ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
            set
            {
                Container = value == null ? null : string.Join(",", value);
            }
        }

        [IgnoreDataMember]
        [XmlIgnore]
        public string[] AudioCodecs
        {
            get
            {
                return (AudioCodec ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
            set
            {
                AudioCodec = value == null ? null : string.Join(",", value);
            }
        }

        [IgnoreDataMember]
        [XmlIgnore]
        public string[] VideoCodecs
        {
            get
            {
                return (VideoCodec ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
            set
            {
                VideoCodec = value == null ? null : string.Join(",", value);
            }
        }

        public string OrgPn { get; set; }
        public string MimeType { get; set; }
        public DlnaProfileType Type { get; set; }

        public List<ProfileCondition> Conditions { get; set; }

        public DirectPlayProfile()
        {
            Conditions = new List<ProfileCondition>();
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
        VideoFramerate
    }
}
