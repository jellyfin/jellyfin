
namespace MediaBrowser.Controller.Dlna
{
    public class MediaProfile
    {
        public string Container { get; set; }
        public string[] AudioCodecs { get; set; }
        public string[] VideoCodecs { get; set; }
        
        public DlnaProfileType Type { get; set; }
        public string OrgPn { get; set; }
        public string MimeType { get; set; }

        public MediaProfile()
        {
            AudioCodecs = new string[] { };
            VideoCodecs = new string[] { };
        }
    }
}
