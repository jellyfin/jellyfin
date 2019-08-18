using System.Collections.Generic;
using MediaBrowser.Providers.Tmdb.Models.General;

namespace MediaBrowser.Providers.Tmdb.Models.TV
{
    public class EpisodeCredits
    {
        public List<Cast> cast { get; set; }
        public List<Crew> crew { get; set; }
        public List<GuestStar> guest_stars { get; set; }
    }
}
