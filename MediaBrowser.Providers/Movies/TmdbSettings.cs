using System.Collections.Generic;

namespace MediaBrowser.Providers.Movies
{
    internal class TmdbImageSettings
    {
        public List<string> backdrop_sizes { get; set; }
        public string secure_base_url { get; set; }
        public List<string> poster_sizes { get; set; }
        public List<string> profile_sizes { get; set; }
    }

    internal class TmdbSettingsResult
    {
        public TmdbImageSettings images { get; set; }
    }
}
