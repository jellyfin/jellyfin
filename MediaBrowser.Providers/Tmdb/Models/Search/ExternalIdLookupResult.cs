using System.Collections.Generic;
using MediaBrowser.Providers.Movies;

namespace MediaBrowser.Providers.Tmdb.Models.Search
{
    public class ExternalIdLookupResult
    {
        public List<TvResult> tv_results { get; set; }
    }
}
