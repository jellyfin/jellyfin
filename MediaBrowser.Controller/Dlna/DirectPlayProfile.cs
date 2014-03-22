using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Dlna
{
    public class DirectPlayProfile
    {
        public string[] Containers { get; set; }
        public string AudioCodec { get; set; }
        public string VideoCodec { get; set; }

        public DlnaProfileType Type { get; set; }

        public List<ProfileCondition> Conditions { get; set; }

        public DirectPlayProfile()
        {
            Conditions = new List<ProfileCondition>();

            Containers = new string[] { };
        }

        public List<string> GetAudioCodecs()
        {
            return (AudioCodec ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
        }

        public List<string> GetVideoCodecs()
        {
            return (VideoCodec ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
        }
    }

    public enum DlnaProfileType
    {
        Audio = 0,
        Video = 1,
        Photo = 2
    }
}
