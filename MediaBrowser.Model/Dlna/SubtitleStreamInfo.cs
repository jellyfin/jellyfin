#nullable disable
#pragma warning disable CS1591

namespace MediaBrowser.Model.Dlna
{
    public class SubtitleStreamInfo
    {
        public string Url { get; set; }

        public string Language { get; set; }

        public string Name { get; set; }

        public bool IsForced { get; set; }

        public string Format { get; set; }

        public string DisplayTitle { get; set; }

        public int Index { get; set; }

        public SubtitleDeliveryMethod DeliveryMethod { get; set; }

        public bool IsExternalUrl { get; set; }
    }
}
