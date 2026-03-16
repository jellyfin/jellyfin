using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Playlists;

/// <summary>
/// Represents a single item in a Jellyfin playlist export.
/// </summary>
public class PlaylistExportItem
{
    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the production year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin item type name (e.g. "Movie", "Episode", "Audio").
    /// </summary>
    public string? ItemType { get; set; }

    /// <summary>
    /// Gets or sets provider IDs (IMDB, TMDB, TVDB, etc.) used for cross-server matching on import.
    /// </summary>
    public Dictionary<string, string>? ProviderIds { get; set; }

    /// <summary>
    /// Gets or sets the series name for episode items.
    /// </summary>
    public string? SeriesName { get; set; }

    /// <summary>
    /// Gets or sets the season number for episode items.
    /// </summary>
    public int? SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number for episode items.
    /// </summary>
    public int? EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the album name for audio items.
    /// </summary>
    public string? Album { get; set; }

    /// <summary>
    /// Gets or sets the artist(s) for audio items.
    /// </summary>
    public string? Artists { get; set; }

    /// <summary>
    /// Gets or sets the runtime in ticks.
    /// </summary>
    public long? RunTimeTicks { get; set; }
}

/// <summary>
/// A portable export of a Jellyfin playlist suitable for backup or cross-server import.
/// Items are identified by provider IDs (IMDB, TMDB, TVDB, etc.) rather than file paths so
/// the playlist can be re-imported on any Jellyfin server that has the same content.
/// </summary>
public class PlaylistExportDto
{
    /// <summary>
    /// Gets or sets the playlist name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the media type string (e.g. "Audio", "Video").
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// Gets or sets the export format version.
    /// </summary>
    public string ExportVersion { get; set; } = "1";

    /// <summary>
    /// Gets or sets the UTC timestamp at which this export was created.
    /// </summary>
    public DateTime ExportedAt { get; set; }

    /// <summary>
    /// Gets or sets the exported playlist items in order.
    /// </summary>
    public List<PlaylistExportItem> Items { get; set; } = [];
}
