using MediaBrowser.Model.Extensions;
using System.Collections.Generic;
using System.Xml.Serialization;
using MediaBrowser.Model.Dlna;
using System.Linq;

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

        private static List<string> SplitValue(string value)
        {
            List<string> list = new List<string>();
            foreach (string i in (value ?? string.Empty).Split(','))
            {
                if (!string.IsNullOrEmpty(i)) list.Add(i);
            }
            return list;
        }

        public List<string> GetCodecs()
        {
            return SplitValue(Codec);
        }

        public List<string> GetContainers()
        {
            return SplitValue(Container);
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

            return codecs.Count == 0 || ListHelper.ContainsIgnoreCase(codecs, SplitValue(codec)[0]);
            //return codecs.Count == 0 || SplitValue(codec).Any(i => ListHelper.ContainsIgnoreCase(codecs, i));
        }
    }
}
