using MediaBrowser.Model.Extensions;
using System.Collections.Generic;
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

        public List<string> GetLanguages()
        {
            List<string> list = new List<string>();
            foreach (string i in (Language ?? string.Empty).Split(','))
            {
                if (!string.IsNullOrEmpty(i)) list.Add(i);
            }
            return list;
        }

        public bool SupportsLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                language = "und";
            }

            List<string> languages = GetLanguages();
            return languages.Count == 0 || ListHelper.ContainsIgnoreCase(languages, language);
        }
    }
}