#pragma warning disable CS1591

using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Lyrics
{
    public interface ILyricManager
    {
        /// <summary>
        /// Gets the lyrics.
        /// </summary>
        /// <param name="item">The media item.</param>
        /// <returns>Lyrics for passed item.</returns>
        LyricResponse GetLyrics(BaseItem item);

        /// <summary>
        /// Checks if requested item has a matching local lyric file.
        /// </summary>
        /// <param name="item">The media item.</param>
        /// <returns>True if item has a matching lyric file; otherwise false.</returns>
        bool HasLyricFile(BaseItem item);
    }
}
