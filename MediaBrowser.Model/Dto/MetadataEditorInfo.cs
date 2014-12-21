using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;

namespace MediaBrowser.Model.Dto
{
    public class MetadataEditorInfo
    {
        public List<ParentalRating> ParentalRatingOptions { get; set; }
        public List<CountryInfo> Countries { get; set; }
        public List<CultureDto> Cultures { get; set; }
        public List<ExternalIdInfo> ExternalIdInfos { get; set; }

        public string ContentType { get; set; }
        public List<NameValuePair> ContentTypeOptions { get; set; }

        public MetadataEditorInfo()
        {
            ParentalRatingOptions = new List<ParentalRating>();
            Countries = new List<CountryInfo>();
            Cultures = new List<CultureDto>();
            ExternalIdInfos = new List<ExternalIdInfo>();
            ContentTypeOptions = new List<NameValuePair>();
        }
    }
}
