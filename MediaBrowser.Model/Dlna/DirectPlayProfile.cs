using System.Collections.Generic;
using System.Xml.Serialization;
using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Model.Dlna
{
    public class DirectPlayProfile
    {
        [XmlAttribute("container")]
        public string Container { get; set; }

        [XmlAttribute("audioCodec")]
        public string AudioCodec { get; set; }

        [XmlAttribute("videoCodec")]
        public string VideoCodec { get; set; }

        [XmlAttribute("type")]
        public DlnaProfileType Type { get; set; }

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
