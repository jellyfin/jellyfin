#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Model.Dto
{
    public class MetadataEditorInfo
    {
        public MetadataEditorInfo()
        {
            ParentalRatingOptions = Array.Empty<ParentalRating>();
            Countries = Array.Empty<CountryInfo>();
            Cultures = Array.Empty<CultureDto>();
            ExternalIdInfos = Array.Empty<ExternalIdInfo>();
            ContentTypeOptions = Array.Empty<NameValuePair>();
        }

        public IReadOnlyList<ParentalRating> ParentalRatingOptions { get; set; }

        public IReadOnlyList<CountryInfo> Countries { get; set; }

        public IReadOnlyList<CultureDto> Cultures { get; set; }

        public IReadOnlyList<ExternalIdInfo> ExternalIdInfos { get; set; }

        public CollectionType? ContentType { get; set; }

        public IReadOnlyList<NameValuePair> ContentTypeOptions { get; set; }
    }
}
