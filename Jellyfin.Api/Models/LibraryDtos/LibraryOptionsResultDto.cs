using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Models.LibraryDtos;

/// <summary>
/// Library options result dto.
/// </summary>
public class LibraryOptionsResultDto
{
    /// <summary>
    /// Gets or sets the metadata savers.
    /// </summary>
    public IReadOnlyList<LibraryOptionInfoDto> MetadataSavers { get; set; } = Array.Empty<LibraryOptionInfoDto>();

    /// <summary>
    /// Gets or sets the metadata readers.
    /// </summary>
    public IReadOnlyList<LibraryOptionInfoDto> MetadataReaders { get; set; } = Array.Empty<LibraryOptionInfoDto>();

    /// <summary>
    /// Gets or sets the subtitle fetchers.
    /// </summary>
    public IReadOnlyList<LibraryOptionInfoDto> SubtitleFetchers { get; set; } = Array.Empty<LibraryOptionInfoDto>();

    /// <summary>
    /// Gets or sets the list of lyric fetchers.
    /// </summary>
    public IReadOnlyList<LibraryOptionInfoDto> LyricFetchers { get; set; } = Array.Empty<LibraryOptionInfoDto>();

    /// <summary>
    /// Gets or sets the list of MediaSegment Providers.
    /// </summary>
    public IReadOnlyList<LibraryOptionInfoDto> MediaSegmentProviders { get; set; } = Array.Empty<LibraryOptionInfoDto>();

    /// <summary>
    /// Gets or sets the type options.
    /// </summary>
    public IReadOnlyList<LibraryTypeOptionsDto> TypeOptions { get; set; } = Array.Empty<LibraryTypeOptionsDto>();
}
