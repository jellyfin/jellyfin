
namespace MediaBrowser.Controller.Dlna
{
    public class TranscodingProfile
    {
        public string Container { get; set; }

        public DlnaProfileType Type { get; set; }

        public string MimeType { get; set; }

        public string VideoCodec { get; set; }

        public string AudioCodec { get; set; }
    }
}
