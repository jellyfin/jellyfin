#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Providers.Plugins.Tmdb.Models.Movies
{
    public class Trailers
    {
        public IReadOnlyList<Youtube> Youtube { get; set; }
    }
}
