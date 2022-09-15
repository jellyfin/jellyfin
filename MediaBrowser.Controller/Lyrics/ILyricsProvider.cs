using System.Collections.Generic;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Lyrics
{
    /// <summary>
    /// Interface ILyricsProvider.
    /// </summary>
    public interface ILyricsProvider
    {
        /// <summary>
        /// Gets the supported media types for this provider.
        /// </summary>
        /// <value>The supported media types.</value>
        IEnumerable<string> SupportedMediaTypes { get; }

        /// <summary>
        /// Gets the lyrics.
        /// </summary>
        /// <param name="item">The item to to process.</param>
        /// <returns>Task{LyricResponse}.</returns>
        LyricResponse? GetLyrics(BaseItem item);
    }
}
