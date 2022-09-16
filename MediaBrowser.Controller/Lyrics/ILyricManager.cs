#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.Lyrics
{
    public interface ILyricManager
    {
        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="lyricProviders">The lyric providers.</param>
        void AddParts(IEnumerable<ILyricProvider> lyricProviders);

        /// <summary>
        /// Gets the lyrics.
        /// </summary>
        /// <param name="item">The media item.</param>
        /// <returns>Lyrics for passed item.</returns>
        LyricResponse GetLyric(BaseItem item);

        /// <summary>
        /// Checks if requested item has a matching local lyric file.
        /// </summary>
        /// <param name="item">The media item.</param>
        /// <returns>True if item has a matching lyrics file; otherwise false.</returns>
        bool HasLyricFile(BaseItem item);
    }
}
