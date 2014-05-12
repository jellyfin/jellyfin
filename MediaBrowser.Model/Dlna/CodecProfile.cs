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
            List<string> list = new List<string>();
            foreach (string i in (Codec ?? string.Empty).Split(','))
            {
                if (!string.IsNullOrEmpty(i)) list.Add(i);
            }
            return list;
        }

        public bool ContainsCodec(string codec)
        {
            List<string> codecs = GetCodecs();

            return codecs.Count == 0 || codecs.Contains(codec, StringComparer.OrdinalIgnoreCase);
        }
    }
}
