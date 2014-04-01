
namespace MediaBrowser.Controller.MediaEncoding
{
    public class VideoEncodingOptions : EncodingOptions
    {
        public string VideoCodec { get; set; }

        public string VideoProfile { get; set; }

        public double? VideoLevel { get; set; }
        
        public int? VideoStreamIndex { get; set; }

        public int? AudioStreamIndex { get; set; }

        public int? SubtitleStreamIndex { get; set; }

        public int? MaxWidth { get; set; }

        public int? MaxHeight { get; set; }

        public int? Height { get; set; }

        public int? Width { get; set; }
    }
}
