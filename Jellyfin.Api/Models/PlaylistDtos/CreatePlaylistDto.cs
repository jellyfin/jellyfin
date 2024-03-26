using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json.Converters;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Api.Models.PlaylistDtos;

/// <summary>
/// Create new playlist dto.
/// </summary>
public class CreatePlaylistDto
{
    /// <summary>
    /// Gets or sets the name of the new playlist.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets item ids to add to the playlist.
    /// </summary>
    [JsonConverter(typeof(JsonCommaDelimitedArrayConverterFactory))]
    public IReadOnlyList<Guid> Ids { get; set; } = [];

    /// <summary>
    /// Gets or sets the user id.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the media type.
    /// </summary>
    public MediaType? MediaType { get; set; }

    /// <summary>
    /// Gets or sets the shares.
    /// </summary>
    public IReadOnlyList<Share> Shares { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether open access is enabled.
    /// </summary>
    public bool OpenAccess { get; set; }
}
