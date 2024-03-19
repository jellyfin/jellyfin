using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Lyrics;

namespace MediaBrowser.Controller.Lyrics;

/// <summary>
/// Interface ILyricParser.
/// </summary>
public interface ILyricParser
{
    /// <summary>
    /// Gets a value indicating the provider name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the priority.
    /// </summary>
    /// <value>The priority.</value>
    ResolverPriority Priority { get; }

    /// <summary>
    /// Parses the raw lyrics into a response.
    /// </summary>
    /// <param name="lyrics">The raw lyrics content.</param>
    /// <returns>The parsed lyrics or null if invalid.</returns>
    LyricDto? ParseLyrics(LyricFile lyrics);
}
