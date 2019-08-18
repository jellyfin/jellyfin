using System.Collections.Generic;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb.Models.Collections
{
    public class CollectionImages
    {
        public List<Backdrop> Backdrops { get; set; }
        public List<Poster> Posters { get; set; }
    }
}
