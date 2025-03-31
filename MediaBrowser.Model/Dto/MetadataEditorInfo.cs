using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Model.Dto;

/// <summary>
/// A class representing metadata editor information.
/// </summary>
public class MetadataEditorInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataEditorInfo"/> class.
    /// </summary>
    public MetadataEditorInfo()
    {
        ParentalRatingOptions = [];
        Countries = [];
        Cultures = [];
        ExternalIdInfos = [];
        ContentTypeOptions = [];
    }

    /// <summary>
    /// Gets or sets the parental rating options.
    /// </summary>
    public IReadOnlyList<ParentalRating> ParentalRatingOptions { get; set; }

    /// <summary>
    /// Gets or sets the countries.
    /// </summary>
    public IReadOnlyList<CountryInfo> Countries { get; set; }

    /// <summary>
    /// Gets or sets the cultures.
    /// </summary>
    public IReadOnlyList<CultureDto> Cultures { get; set; }

    /// <summary>
    /// Gets or sets the external id infos.
    /// </summary>
    public IReadOnlyList<ExternalIdInfo> ExternalIdInfos { get; set; }

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public CollectionType? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the content type options.
    /// </summary>
    public IReadOnlyList<NameValuePair> ContentTypeOptions { get; set; }
}
