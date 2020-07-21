#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Providers.Plugins.Tmdb.Models.General;

namespace MediaBrowser.Providers.Plugins.Tmdb.Models.TV
{
    public class Credits
    {
        public List<Cast> Cast { get; set; }

        public List<Crew> Crew { get; set; }
    }
}
