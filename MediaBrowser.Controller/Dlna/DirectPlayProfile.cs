
namespace MediaBrowser.Controller.Dlna
{
    public class DirectPlayProfile
    {
        public string[] Containers { get; set; }
        public string[] AudioCodecs { get; set; }
        public string[] VideoCodecs { get; set; }
        public string MimeType { get; set; }
        public DlnaProfileType Type { get; set; }

        public DirectPlayProfile()
        {
            Containers = new string[] { };
            AudioCodecs = new string[] { };
            VideoCodecs = new string[] { };
        }
    }

    public enum DlnaProfileType
    {
        Audio = 0,
        Video = 1
    }
}
