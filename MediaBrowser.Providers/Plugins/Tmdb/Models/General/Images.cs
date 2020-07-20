#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Providers.Plugins.Tmdb.Models.General
{
    public class Images
    {
        public List<Backdrop> Backdrops { get; set; }

        public List<Poster> Posters { get; set; }
    }
}
