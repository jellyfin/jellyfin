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
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets item ids to add to the playlist.
    /// </summary>
    [JsonConverter(typeof(JsonCommaDelimitedCollectionConverterFactory))]
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
    /// Gets or sets the playlist users.
    /// </summary>
    public IReadOnlyList<PlaylistUserPermissions> Users { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the playlist is public.
    /// </summary>
    public bool IsPublic { get; set; } = true;
}
