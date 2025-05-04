using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Jellyfin.Extensions.Json.Converters;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Api.Models.PlaylistDtos;

/// <summary>
/// Update existing playlist dto. Fields set to `null` will not be updated and keep their current values.
/// </summary>
public class UpdatePlaylistDto
{
    /// <summary>
    /// Gets or sets the name of the new playlist.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets item ids of the playlist.
    /// </summary>
    [JsonConverter(typeof(JsonCommaDelimitedCollectionConverterFactory))]
    public IReadOnlyList<Guid>? Ids { get; set; }

    /// <summary>
    /// Gets or sets the playlist users.
    /// </summary>
    public IReadOnlyList<PlaylistUserPermissions>? Users { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the playlist is public.
    /// </summary>
    public bool? IsPublic { get; set; }
}
