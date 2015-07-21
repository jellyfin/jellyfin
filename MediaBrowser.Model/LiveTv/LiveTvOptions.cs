using System.Collections.Generic;

namespace MediaBrowser.Model.LiveTv
{
    public class LiveTvOptions
    {
        public int? GuideDays { get; set; }
        public bool EnableMovieProviders { get; set; }
        public List<TunerHostInfo> TunerHosts { get; set; }
        public string RecordingPath { get; set; }

        public LiveTvOptions()
        {
            EnableMovieProviders = true;
            TunerHosts = new List<TunerHostInfo>();
        }
    }

    public class TunerHostInfo
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }
    }
}