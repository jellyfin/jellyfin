using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class EpisodeInfo : ItemLookupInfo
    {
        public Dictionary<string, string> SeriesProviderIds { get; set; }

        public int? IndexNumberEnd { get; set; }
        public int? AnimeSeriesIndex { get; set; }

        public bool IsMissingEpisode { get; set; }
        public bool IsVirtualUnaired { get; set; }

        public EpisodeInfo()
        {
            SeriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}