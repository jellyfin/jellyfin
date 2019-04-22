using System;
using Jellyfin.Model.Entities;
using Jellyfin.Model.Globalization;
using Jellyfin.Model.Providers;

namespace Jellyfin.Model.Dto
{
    public class MetadataEditorInfo
    {
        public ParentalRating[] ParentalRatingOptions { get; set; }
        public CountryInfo[] Countries { get; set; }
        public CultureDto[] Cultures { get; set; }
        public ExternalIdInfo[] ExternalIdInfos { get; set; }

        public string ContentType { get; set; }
        public NameValuePair[] ContentTypeOptions { get; set; }

        public MetadataEditorInfo()
        {
            ParentalRatingOptions = Array.Empty<ParentalRating>();
            Countries = Array.Empty<CountryInfo>();
            Cultures = Array.Empty<CultureDto>();
            ExternalIdInfos = Array.Empty<ExternalIdInfo>();
            ContentTypeOptions = Array.Empty<NameValuePair>();
        }
    }
}
