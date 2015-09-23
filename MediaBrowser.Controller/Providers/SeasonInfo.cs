using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class SeasonInfo : ItemLookupInfo
    {
        public Dictionary<string, string> SeriesProviderIds { get; set; }
        public int? AnimeSeriesIndex { get; set; }

        public SeasonInfo()
        {
            SeriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}