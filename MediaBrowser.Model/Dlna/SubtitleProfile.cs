using MediaBrowser.Model.Extensions;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    public class SubtitleProfile
    {
        public string Format { get; set; }

        public SubtitleDeliveryMethod Method { get; set; }

        public string DidlMode { get; set; }

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

            List<string> languages = GetLanguages();
            return languages.Count == 0 || ListHelper.ContainsIgnoreCase(languages, subLanguage);
        }
    }
}