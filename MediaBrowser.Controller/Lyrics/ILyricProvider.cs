using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Resolvers;

namespace MediaBrowser.Controller.Lyrics;

/// <summary>
/// Interface ILyricsProvider.
/// </summary>
public interface ILyricProvider
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
    /// Gets the supported media types for this provider.
    /// </summary>
    /// <value>The supported media types.</value>
    IEnumerable<string> SupportedMediaTypes { get; }

    /// <summary>
    /// Gets the lyrics.
    /// </summary>
    /// <param name="item">The media item.</param>
    /// <returns>If found, returns lyrics for passed item; otherwise, null.</returns>
    LyricResponse? GetLyrics(BaseItem item);
}
