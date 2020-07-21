#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Providers.Plugins.Tmdb.Models.General;

namespace MediaBrowser.Providers.Plugins.Tmdb.Models.TV
{
    public class EpisodeCredits
    {
        public List<Cast> Cast { get; set; }

        public List<Crew> Crew { get; set; }

        public List<GuestStar> Guest_Stars { get; set; }
    }
}
