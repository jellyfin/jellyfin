using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class EpisodeInfo : ItemLookupInfo
    {
        public Dictionary<string, string> SeriesProviderIds { get; set; }

        public int? IndexNumberEnd { get; set; }

        public bool IsMissingEpisode { get; set; }

        public string SeriesDisplayOrder { get; set; }

        public EpisodeInfo()
        {
            SeriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
