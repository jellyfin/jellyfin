using System.Collections.Generic;

namespace MediaBrowser.Providers.Tmdb.Movies
{
    internal class TmdbImageSettings
    {
        public List<string> backdrop_sizes { get; set; }
        public string secure_base_url { get; set; }
        public List<string> poster_sizes { get; set; }
        public List<string> profile_sizes { get; set; }

        public string GetImageUrl(string image)
        {
            return secure_base_url + image;
        }
    }

    internal class TmdbSettingsResult
    {
        public TmdbImageSettings images { get; set; }
    }
}
