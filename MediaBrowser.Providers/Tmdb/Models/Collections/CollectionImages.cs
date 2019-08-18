using System.Collections.Generic;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb.Models.Collections
{
    public class CollectionImages
    {
        public List<Backdrop> backdrops { get; set; }
        public List<Poster> posters { get; set; }
    }
}
