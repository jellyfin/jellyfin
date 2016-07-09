using MediaBrowser.Model.Extensions;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    public class CodecProfile
    {
        [XmlAttribute("type")]
        public CodecType Type { get; set; }
       
        public ProfileCondition[] Conditions { get; set; }

        public ProfileCondition[] ApplyConditions { get; set; }

        [XmlAttribute("codec")]
        public string Codec { get; set; }

        [XmlAttribute("container")]
        public string Container { get; set; }

        public CodecProfile()
        {
            Conditions = new ProfileCondition[] {};
            ApplyConditions = new ProfileCondition[] { };
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

        public List<string> GetContainers()
        {
            List<string> list = new List<string>();
            foreach (string i in (Container ?? string.Empty).Split(','))
            {
                if (!string.IsNullOrEmpty(i)) list.Add(i);
            }
            return list;
        }

        private bool ContainsContainer(string container)
        {
            List<string> containers = GetContainers();

            return containers.Count == 0 || ListHelper.ContainsIgnoreCase(containers, container ?? string.Empty);
        }

        public bool ContainsCodec(string codec, string container)
        {
            if (!ContainsContainer(container))
            {
                return false;
            }

            List<string> codecs = GetCodecs();

            return codecs.Count == 0 || ListHelper.ContainsIgnoreCase(codecs, codec);
        }
    }
}
