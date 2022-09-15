using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Mvc;

namespace MediaBrowser.Controller.Lyrics
{
    /// <summary>
    /// Item helper.
    /// </summary>
    public class LyricInfo
    {
        /// <summary>
        /// Opens lyrics file, converts to a List of Lyrics, and returns it.
        /// </summary>
        /// <param name="lyricProviders">Collection of all registered <see cref="ILyricsProvider"/> interfaces.</param>
        /// <param name="item">Requested Item.</param>
        /// <returns>Collection of Lyrics.</returns>
        public static LyricResponse? GetLyricData(IEnumerable<ILyricsProvider> lyricProviders, BaseItem item)
        {

            foreach (var provider in lyricProviders)
            {
                var result = provider.GetLyrics(item);
                if (result is not null)
                {
                    return result;
                }
            }

            return new LyricResponse
            {
                Lyrics = new List<Lyric>
                {
                    new Lyric { Start = 0, Text = "Test" }
                }
            };
        }

        /// <summary>
        /// Checks if requested item has a matching lyric file.
        /// </summary>
        /// <param name="lyricProvider">The current lyricProvider interface.</param>
        /// <param name="itemPath">Path of requested item.</param>
        /// <returns>True if item has a matching lyrics file.</returns>
        public static string? GetLyricFilePath(ILyricsProvider lyricProvider, string itemPath)
        {
            if (lyricProvider.SupportedMediaTypes.Any())
            {
                foreach (string lyricFileExtension in lyricProvider.SupportedMediaTypes)
                {
                    string lyricFilePath = @Path.ChangeExtension(itemPath, lyricFileExtension);
                    if (System.IO.File.Exists(lyricFilePath))
                    {
                        return lyricFilePath;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if requested item has a matching local lyric file.
        /// </summary>
        /// <param name="lyricProviders">Collection of all registered <see cref="ILyricsProvider"/> interfaces.</param>
        /// <param name="itemPath">Path of requested item.</param>
        /// <returns>True if item has a matching lyrics file; otherwise false.</returns>
        public static bool HasLyricFile(IEnumerable<ILyricsProvider> lyricProviders, string itemPath)
        {
            foreach (var provider in lyricProviders)
            {
                if (GetLyricFilePath(provider, itemPath) is not null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
