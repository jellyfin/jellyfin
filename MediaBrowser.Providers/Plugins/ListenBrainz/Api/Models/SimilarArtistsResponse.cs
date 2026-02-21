using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MediaBrowser.Providers.Plugins.ListenBrainz.Api.Models;

/// <summary>
/// Response from ListenBrainz Labs similar-artists endpoint.
/// </summary>
public class SimilarArtistsResponse
{
    /// <summary>
    /// Gets or sets the list of similar artists.
    /// </summary>
    [JsonPropertyName("data")]
    public IReadOnlyList<SimilarArtistData>? Data { get; set; }
}
