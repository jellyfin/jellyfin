using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// Lyric search request.
/// </summary>
public class LyricSearchRequest : IHasProviderIds
{
    /// <summary>
    /// Gets or sets the media path.
    /// </summary>
    public string? MediaPath { get; set; }

    /// <summary>
    /// Gets or sets the artist name.
    /// </summary>
    public IReadOnlyList<string>? ArtistNames { get; set; }

    /// <summary>
    /// Gets or sets the album name.
    /// </summary>
    public string? AlbumName { get; set; }

    /// <summary>
    /// Gets or sets the song name.
    /// </summary>
    public string? SongName { get; set; }

    /// <summary>
    /// Gets or sets the track duration in ticks.
    /// </summary>
    public long? Duration { get; set; }

    /// <inheritdoc />
    public Dictionary<string, string> ProviderIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a value indicating whether to search all providers.
    /// </summary>
    public bool SearchAllProviders { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of disabled lyric fetcher names.
    /// </summary>
    public IReadOnlyList<string> DisabledLyricFetchers { get; set; } = [];

    /// <summary>
    /// Gets or sets the order of lyric fetchers.
    /// </summary>
    public IReadOnlyList<string> LyricFetcherOrder { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether this request is automated.
    /// </summary>
    public bool IsAutomated { get; set; }
}
