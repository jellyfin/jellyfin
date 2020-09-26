#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Providers.Plugins.Tmdb.Movies
{
    internal class TmdbImageSettings
    {
        public IReadOnlyList<string> backdrop_sizes { get; set; }

        public string secure_base_url { get; set; }

        public IReadOnlyList<string> poster_sizes { get; set; }

        public IReadOnlyList<string> profile_sizes { get; set; }

        public string GetImageUrl(string image)
        {
            return secure_base_url + image;
        }
    }
}
