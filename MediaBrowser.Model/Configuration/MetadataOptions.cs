#nullable disable
#pragma warning disable CS1591, CA1819

using System;

namespace MediaBrowser.Model.Configuration;

/// <summary>
/// Class MetadataOptions.
/// </summary>
public class MetadataOptions
{
    public MetadataOptions()
    {
        DisabledMetadataSavers = [];
        LocalMetadataReaderOrder = [];
        DisabledMetadataFetchers = [];
        MetadataFetcherOrder = [];
        DisabledImageFetchers = [];
        ImageFetcherOrder = [];
    }

    public string ItemType { get; set; }

    public string[] DisabledMetadataSavers { get; set; }

    public string[] LocalMetadataReaderOrder { get; set; }

    public string[] DisabledMetadataFetchers { get; set; }

    public string[] MetadataFetcherOrder { get; set; }

    public string[] DisabledImageFetchers { get; set; }

    public string[] ImageFetcherOrder { get; set; }
}
