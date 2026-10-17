namespace MediaBrowser.Controller.Library;

/// <summary>
/// A reference to a similar item by provider ID with a similarity score.
/// </summary>
public class SimilarItemReference
{
    /// <summary>
    /// Gets or sets the provider name (e.g., "Tmdb", "MusicBrainzArtist").
    /// </summary>
    public required string ProviderName { get; set; }

    /// <summary>
    /// Gets or sets the provider ID value.
    /// </summary>
    public required string ProviderId { get; set; }

    /// <summary>
    /// Gets or sets the similarity score (0.0 to 1.0).
    /// </summary>
    public float? Score { get; set; }
}
