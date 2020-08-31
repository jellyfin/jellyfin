#pragma warning disable CS1591

namespace MediaBrowser.Controller.MediaEncoding
{
    public class ImageEncodingOptions
    {
        public string InputPath { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public int? MaxWidth { get; set; }

        public int? MaxHeight { get; set; }

        public int? Quality { get; set; }

        public string Format { get; set; }
    }
}
