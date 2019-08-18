using System.Collections.Generic;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb.Models.TV
{
    public class Credits
    {
        public List<Cast> Cast { get; set; }
        public List<Crew> Crew { get; set; }
    }
}
