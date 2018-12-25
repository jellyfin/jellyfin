using MediaBrowser.Model.Extensions;
using System;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Class MetadataOptions.
    /// </summary>
    public class MetadataOptions
    {
        public string ItemType { get; set; }

        public string[] DisabledMetadataSavers { get; set; }
        public string[] LocalMetadataReaderOrder { get; set; }

        public string[] DisabledMetadataFetchers { get; set; }
        public string[] MetadataFetcherOrder { get; set; }

        public string[] DisabledImageFetchers { get; set; }
        public string[] ImageFetcherOrder { get; set; }

        public MetadataOptions()
        {
            DisabledMetadataSavers = new string[] { };
            LocalMetadataReaderOrder = new string[] { };

            DisabledMetadataFetchers = new string[] { };
            MetadataFetcherOrder = new string[] { };
            DisabledImageFetchers = new string[] { };
            ImageFetcherOrder = new string[] { };
        }

        public bool IsMetadataSaverEnabled(string name)
        {
            return !ListHelper.ContainsIgnoreCase(DisabledMetadataSavers, name);
        }
    }
}
