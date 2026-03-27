using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Library;

/// <summary>
/// Describes a single item provided by an <see cref="MediaBrowser.Controller.Library.IExternalItemProvider"/>.
/// </summary>
public class ExternalItemInfo
{
    /// <summary>
    /// Gets or sets the stable identifier assigned by the external provider.
    /// </summary>
    public required string ExternalId { get; set; }

    /// <summary>
    /// Gets or sets the kind of library item.
    /// </summary>
    public required BaseItemKind ItemKind { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the production year.
    /// </summary>
    public int? ProductionYear { get; set; }

    /// <summary>
    /// Gets or sets the overview.
    /// </summary>
    public string? Overview { get; set; }

    /// <summary>
    /// Gets or sets the official content rating (e.g. "PG-13").
    /// </summary>
    public string? OfficialRating { get; set; }

    /// <summary>
    /// Gets or sets the container format (e.g. "mkv", "mp4").
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Gets or sets the runtime in ticks.
    /// </summary>
    public long? RunTimeTicks { get; set; }

    /// <summary>
    /// Gets or sets the genre names.
    /// </summary>
    public IReadOnlyList<string> Genres { get; set; } = [];

    /// <summary>
    /// Gets or sets the provider IDs (e.g. TMDB, TVDB) for metadata matching.
    /// </summary>
    public IReadOnlyDictionary<string, string> ProviderIds { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the media streams. Pre-populated from the remote probe so local probing is not required.
    /// </summary>
    public IReadOnlyList<MediaStream> MediaStreams { get; set; } = [];

    /// <summary>
    /// Gets or sets the <see cref="ExternalId"/> of the parent series. Applies to episodes.
    /// </summary>
    public string? SeriesExternalId { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ExternalId"/> of the parent season. Applies to episodes.
    /// </summary>
    public string? SeasonExternalId { get; set; }

    /// <summary>
    /// Gets or sets the episode or track number within its parent.
    /// </summary>
    public int? IndexNumber { get; set; }

    /// <summary>
    /// Gets or sets the season number. Applies to episodes.
    /// </summary>
    public int? ParentIndexNumber { get; set; }

    /// <summary>
    /// Gets or sets the URL of the primary (poster) image on the remote server.
    /// The local server will fetch and cache this image.
    /// </summary>
    public string? PrimaryImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL of the backdrop image on the remote server.
    /// </summary>
    public string? BackdropImageUrl { get; set; }
}
