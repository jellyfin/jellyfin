using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;

namespace MediaBrowser.Model.Dto
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
            ParentalRatingOptions = new ParentalRating[] { };
            Countries = new CountryInfo[] { };
            Cultures = new CultureDto[] { };
            ExternalIdInfos = new ExternalIdInfo[] { };
            ContentTypeOptions = new NameValuePair[] { };
        }
    }
}
