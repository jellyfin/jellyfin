using System;
using System.Text.Json.Serialization;

namespace MediaBrowser.Providers.Plugins.ListenBrainz.Api.Models;

/// <summary>
/// A similar artist data entry from the ListenBrainz Labs API.
/// </summary>
public class SimilarArtistData
{
    /// <summary>
    /// Gets or sets the MusicBrainz artist ID.
    /// </summary>
    [JsonPropertyName("artist_mbid")]
    public Guid ArtistMbid { get; set; }

    /// <summary>
    /// Gets or sets the artist name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the similarity score.
    /// </summary>
    [JsonPropertyName("score")]
    public double Score { get; set; }
}
