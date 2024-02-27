#nullable disable

#pragma warning disable CA2227, CS1591

using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Controller.Providers
{
    public class EpisodeInfo : ItemLookupInfo
    {
        public EpisodeInfo()
        {
            SeriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            SeasonProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public Dictionary<string, string> SeriesProviderIds { get; set; }

        public Dictionary<string, string> SeasonProviderIds { get; set; }

        public int? IndexNumberEnd { get; set; }

        public bool IsMissingEpisode { get; set; }

        public string SeriesDisplayOrder { get; set; }
    }
}
