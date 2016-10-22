using System.Collections.Generic;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    public class ResponseProfile
    {
        public string Container { get; set; }

        public string AudioCodec { get; set; }

        public string VideoCodec { get; set; }

        public DlnaProfileType Type { get; set; }

        public string OrgPn { get; set; }

        public string MimeType { get; set; }

        public ProfileCondition[] Conditions { get; set; }

        public ResponseProfile()
        {
            Conditions = new ProfileCondition[] {};
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

        public List<string> GetAudioCodecs()
        {
            List<string> list = new List<string>();
            foreach (string i in (AudioCodec ?? string.Empty).Split(','))
            {
                if (!string.IsNullOrEmpty(i)) list.Add(i);
            }
            return list;
        }

        public List<string> GetVideoCodecs()
        {
            List<string> list = new List<string>();
            foreach (string i in (VideoCodec ?? string.Empty).Split(','))
            {
                if (!string.IsNullOrEmpty(i)) list.Add(i);
            }
            return list;
        }
    }
}
