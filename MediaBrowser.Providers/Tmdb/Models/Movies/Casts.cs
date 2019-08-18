using System.Collections.Generic;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb.Models.Movies
{
    public class Casts
    {
        public List<Cast> cast { get; set; }
        public List<Crew> crew { get; set; }
    }
}
