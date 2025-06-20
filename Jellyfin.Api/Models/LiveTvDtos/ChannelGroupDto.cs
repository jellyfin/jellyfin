using System;
using MediaBrowser.Model.Dto;

namespace Jellyfin.Api.Models.LiveTvDtos;

/// <summary>
/// Channel group dto.
/// </summary>
public class ChannelGroupDto
{
    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of channels in this group.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the channels in this group.
    /// </summary>
    public BaseItemDto[] Channels { get; set; } = Array.Empty<BaseItemDto>();
}
