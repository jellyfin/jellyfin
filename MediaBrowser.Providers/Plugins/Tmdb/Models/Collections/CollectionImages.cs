#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Providers.Plugins.Tmdb.Models.General;

namespace MediaBrowser.Providers.Plugins.Tmdb.Models.Collections
{
    public class CollectionImages
    {
        public List<Backdrop> Backdrops { get; set; }

        public List<Poster> Posters { get; set; }
    }
}
