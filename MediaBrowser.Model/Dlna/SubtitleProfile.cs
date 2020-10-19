#nullable disable
#pragma warning disable CS1591

using System;
using System.Linq;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    public class SubtitleProfile
    {
        [XmlAttribute("format")]
        public string Format { get; set; }

        [XmlAttribute("method")]
        public SubtitleDeliveryMethod Method { get; set; }

        [XmlAttribute("didlMode")]
        public string DidlMode { get; set; }

        [XmlAttribute("language")]
        public string Language { get; set; }

        [XmlAttribute("container")]
        public string Container { get; set; }

        public string[] GetLanguages()
        {
            return ContainerProfile.SplitValue(Language);
        }

        public bool SupportsLanguage(string subLanguage)
        {
            if (string.IsNullOrEmpty(Language))
            {
                return true;
            }

            if (string.IsNullOrEmpty(subLanguage))
            {
                subLanguage = "und";
            }

            var languages = GetLanguages();
            return languages.Length == 0 || languages.Contains(subLanguage, StringComparer.OrdinalIgnoreCase);
        }
    }
}
