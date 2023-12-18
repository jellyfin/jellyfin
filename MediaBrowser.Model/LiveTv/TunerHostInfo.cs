#nullable disable
#pragma warning disable CS1591

namespace MediaBrowser.Model.LiveTv
{
    public class TunerHostInfo
    {
        public TunerHostInfo()
        {
            AllowHWTranscoding = true;
            IgnoreDts = true;
        }

        public string Id { get; set; }

        public string Url { get; set; }

        public string Type { get; set; }

        public string DeviceId { get; set; }

        public string FriendlyName { get; set; }

        public bool ImportFavoritesOnly { get; set; }

        public bool AllowHWTranscoding { get; set; }

        public bool EnableStreamLooping { get; set; }

        public string Source { get; set; }

        public int TunerCount { get; set; }

        public string UserAgent { get; set; }

        public bool IgnoreDts { get; set; }
    }
}
