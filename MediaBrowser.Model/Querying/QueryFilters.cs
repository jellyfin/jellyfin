#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.Querying
{
    public class QueryFilters
    {
        public QueryFilters()
        {
            Tags = Array.Empty<string>();
            Genres = Array.Empty<NameGuidPair>();
            AudioLanguages = Array.Empty<NameValuePair>();
            SubtitleLanguages = Array.Empty<NameValuePair>();
        }

        public IReadOnlyList<NameGuidPair> Genres { get; set; }

        public IReadOnlyList<string> Tags { get; set; }

        public IReadOnlyList<NameValuePair> AudioLanguages { get; set; }

        public IReadOnlyList<NameValuePair> SubtitleLanguages { get; set; }
    }
}
