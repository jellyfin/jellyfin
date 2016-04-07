using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Subtitles
{
    public class SubtitleSearchRequest : IHasProviderIds
    {
        public string Language { get; set; }

        public VideoContentType ContentType { get; set; }

        public string MediaPath { get; set; }
        public string SeriesName { get; set; }
        public string Name { get; set; }
        public int? IndexNumber { get; set; }
        public int? IndexNumberEnd { get; set; }
        public int? ParentIndexNumber { get; set; }
        public int? ProductionYear { get; set; }
        public long? RuntimeTicks { get; set; }
        public bool IsPerfectMatch { get; set; }
        public Dictionary<string, string> ProviderIds { get; set; }

        public bool SearchAllProviders { get; set; }

        public SubtitleSearchRequest()
        {
            SearchAllProviders = true;
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}