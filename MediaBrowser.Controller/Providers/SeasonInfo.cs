#pragma warning disable CA2227, CS1591
#nullable disable

using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class SeasonInfo : ItemLookupInfo
    {
        public SeasonInfo()
        {
            SeriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public Dictionary<string, string> SeriesProviderIds { get; set; }

        public string SeriesDisplayOrder { get; set; }
    }
}
