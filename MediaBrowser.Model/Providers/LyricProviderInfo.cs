namespace MediaBrowser.Model.Providers;

/// <summary>
/// Lyric provider info.
/// </summary>
public class LyricProviderInfo
{
    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the provider id.
    /// </summary>
    public required string Id { get; init; }
}
