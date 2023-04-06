using System;

namespace Jellyfin.Api.Models.SyncPlayDtos;

/// <summary>
/// Class JoinGroupRequestDto.
/// </summary>
public class JoinGroupRequestDto
{
    /// <summary>
    /// Gets or sets the group identifier.
    /// </summary>
    /// <value>The identifier of the group to join.</value>
    public Guid GroupId { get; set; }
}
