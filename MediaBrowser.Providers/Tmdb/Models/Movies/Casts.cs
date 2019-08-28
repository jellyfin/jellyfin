using System.Collections.Generic;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb.Models.Movies
{
    public class Casts
    {
        public List<Cast> Cast { get; set; }
        public List<Crew> Crew { get; set; }
    }
}
