using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Library;

/// <summary>
/// Describes a single item provided by an external item provider.
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
    /// Gets or sets the playback path (typically an HTTP URL on the remote server).
    /// When set, the item appears as a playable media source instead of a placeholder.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the runtime in ticks.
    /// </summary>
    public long? RunTimeTicks { get; set; }

    /// <summary>
    /// Gets or sets the premiere or release date.
    /// </summary>
    public DateTime? PremiereDate { get; set; }

    /// <summary>
    /// Gets or sets the end date (e.g. when a series stopped airing).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the community rating (e.g. IMDb score).
    /// </summary>
    public float? CommunityRating { get; set; }

    /// <summary>
    /// Gets or sets the critic rating (e.g. Rotten Tomatoes score).
    /// </summary>
    public float? CriticRating { get; set; }

    /// <summary>
    /// Gets or sets the tagline.
    /// </summary>
    public string? Tagline { get; set; }

    /// <summary>
    /// Gets or sets the genre names.
    /// </summary>
    public IReadOnlyList<string> Genres { get; set; } = [];

    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the studio names.
    /// </summary>
    public IReadOnlyList<string> Studios { get; set; } = [];

    /// <summary>
    /// Gets or sets the original title in the original language.
    /// </summary>
    public string? OriginalTitle { get; set; }

    /// <summary>
    /// Gets or sets the official home page URL.
    /// </summary>
    public string? HomePageUrl { get; set; }

    /// <summary>
    /// Gets or sets the production/filming locations.
    /// </summary>
    public IReadOnlyList<string> ProductionLocations { get; set; } = [];

    /// <summary>
    /// Gets or sets the provider IDs (e.g. TMDB, TVDB) for metadata matching.
    /// </summary>
    public IReadOnlyDictionary<string, string> ProviderIds { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the media streams. Pre-populated from the remote probe so local probing is not required.
    /// </summary>
    public IReadOnlyList<MediaStream> MediaStreams { get; set; } = [];

    /// <summary>
    /// Gets or sets the artist names. Applies to audio items.
    /// </summary>
    public IReadOnlyList<string> Artists { get; set; } = [];

    /// <summary>
    /// Gets or sets the album artist names. Applies to audio items and albums.
    /// </summary>
    public IReadOnlyList<string> AlbumArtists { get; set; } = [];

    /// <summary>
    /// Gets or sets the series status (e.g. Continuing, Ended). Applies to series.
    /// </summary>
    public string? SeriesStatus { get; set; }

    /// <summary>
    /// Gets or sets the episode display order (e.g. "aired", "dvd", "absolute"). Applies to series.
    /// </summary>
    public string? DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the TMDB collection/franchise name. Applies to movies.
    /// </summary>
    public string? TmdbCollectionName { get; set; }

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
    /// Gets or sets the ending episode number for multi-episode entries. Applies to episodes.
    /// </summary>
    public int? IndexNumberEnd { get; set; }

    /// <summary>
    /// Gets or sets the season number this special airs before. Applies to special episodes.
    /// </summary>
    public int? AirsBeforeSeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the season number this special airs after. Applies to special episodes.
    /// </summary>
    public int? AirsAfterSeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number this special airs before. Applies to special episodes.
    /// </summary>
    public int? AirsBeforeEpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the URL of the primary (poster) image on the remote server.
    /// </summary>
    public string? PrimaryImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the URLs of backdrop images on the remote server.
    /// </summary>
    public IReadOnlyList<string> BackdropImageUrls { get; set; } = [];

    /// <summary>
    /// Gets or sets the URL of the logo image on the remote server.
    /// </summary>
    public string? LogoImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL of the thumb (landscape) image on the remote server.
    /// </summary>
    public string? ThumbImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL of the banner image on the remote server.
    /// </summary>
    public string? BannerImageUrl { get; set; }
}
