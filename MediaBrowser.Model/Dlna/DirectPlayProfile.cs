using System.Collections.Generic;

namespace MediaBrowser.Model.Dlna
{
    public class DirectPlayProfile
    {
        public string Container { get; set; }

        public string AudioCodec { get; set; }

        public string VideoCodec { get; set; }

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
