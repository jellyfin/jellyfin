using System.Collections.Generic;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Api.Models.LibraryDtos;

/// <summary>
/// Library type options dto.
/// </summary>
public class LibraryTypeOptionsDto
{
    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the metadata fetchers.
    /// </summary>
    public IReadOnlyList<LibraryOptionInfoDto> MetadataFetchers { get; set; } = [];

    /// <summary>
    /// Gets or sets the image fetchers.
    /// </summary>
    public IReadOnlyList<LibraryOptionInfoDto> ImageFetchers { get; set; } = [];

    /// <summary>
    /// Gets or sets the similar item providers.
    /// </summary>
    public IReadOnlyList<LibraryOptionInfoDto> SimilarItemProviders { get; set; } = [];

    /// <summary>
    /// Gets or sets the supported image types.
    /// </summary>
    public IReadOnlyList<ImageType> SupportedImageTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the default image options.
    /// </summary>
    public IReadOnlyList<ImageOption> DefaultImageOptions { get; set; } = [];
}
