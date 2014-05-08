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
}
