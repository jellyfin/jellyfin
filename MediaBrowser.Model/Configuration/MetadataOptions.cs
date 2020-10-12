#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    ///     Class MetadataOptions.
    /// </summary>
    public class MetadataOptions
    {
        public MetadataOptions()
        {
            DisabledMetadataSavers = Array.Empty<string>();
            LocalMetadataReaderOrder = Array.Empty<string>();
            DisabledMetadataFetchers = Array.Empty<string>();
            MetadataFetcherOrder = Array.Empty<string>();
            DisabledImageFetchers = Array.Empty<string>();
            ImageFetcherOrder = Array.Empty<string>();
        }

        public string ItemType { get; set; }

        public IReadOnlyCollection<string> DisabledMetadataSavers { get; set; }

        public IReadOnlyCollection<string> LocalMetadataReaderOrder { get; set; }

        public IReadOnlyCollection<string> DisabledMetadataFetchers { get; set; }

        public IReadOnlyCollection<string> MetadataFetcherOrder { get; set; }

        public IReadOnlyCollection<string> DisabledImageFetchers { get; set; }

        public IReadOnlyCollection<string> ImageFetcherOrder { get; set; }
    }
}
