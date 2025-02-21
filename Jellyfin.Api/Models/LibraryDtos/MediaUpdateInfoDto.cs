using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Models.LibraryDtos;

/// <summary>
/// Media Update Info Dto.
/// </summary>
public class MediaUpdateInfoDto
{
    /// <summary>
    /// Gets or sets the list of updates.
    /// </summary>
    public IReadOnlyList<MediaUpdateInfoPathDto> Updates { get; set; } = Array.Empty<MediaUpdateInfoPathDto>();
}
