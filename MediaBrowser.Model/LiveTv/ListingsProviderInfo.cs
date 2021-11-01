#nullable disable
#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.LiveTv
{
    public class ListingsProviderInfo
    {
        public ListingsProviderInfo()
        {
            NewsCategories = new[] { "news", "journalism", "documentary", "current affairs" };
            SportsCategories = new[] { "sports", "basketball", "baseball", "football" };
            KidsCategories = new[] { "kids", "family", "children", "childrens", "disney" };
            MovieCategories = new[] { "movie" };
            EnabledTuners = Array.Empty<string>();
            EnableAllTuners = true;
            ChannelMappings = Array.Empty<NameValuePair>();
        }

        public string Id { get; set; }

        public string Type { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string ListingsId { get; set; }

        public string ZipCode { get; set; }

        public string Country { get; set; }

        public string Path { get; set; }

        public string[] EnabledTuners { get; set; }

        public bool EnableAllTuners { get; set; }

        public string[] NewsCategories { get; set; }

        public string[] SportsCategories { get; set; }

        public string[] KidsCategories { get; set; }

        public string[] MovieCategories { get; set; }

        public NameValuePair[] ChannelMappings { get; set; }

        public string MoviePrefix { get; set; }

        public string PreferredLanguage { get; set; }

        public string UserAgent { get; set; }
    }
}
