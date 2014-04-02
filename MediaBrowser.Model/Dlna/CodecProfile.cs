using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    public class CodecProfile
    {
        [XmlAttribute("type")]
        public CodecType Type { get; set; }
       
        public ProfileCondition[] Conditions { get; set; }

        [XmlAttribute("codec")]
        public string Codec { get; set; }

        public CodecProfile()
        {
            Conditions = new ProfileCondition[] {};
        }

        public List<string> GetCodecs()
        {
            return (Codec ?? string.Empty).Split(',').Where(i => !string.IsNullOrEmpty(i)).ToList();
        }

        public bool ContainsCodec(string codec)
        {
            var codecs = GetCodecs();

            return codecs.Count == 0 || codecs.Contains(codec, StringComparer.OrdinalIgnoreCase);
        }
    }

    public enum CodecType
    {
        Video = 0,
        VideoAudio = 1,
        Audio = 2
    }

    public class ProfileCondition
    {
        [XmlAttribute("condition")]
        public ProfileConditionType Condition { get; set; }

        [XmlAttribute("property")]
        public ProfileConditionValue Property { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }

        [XmlAttribute("isRequired")]
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
