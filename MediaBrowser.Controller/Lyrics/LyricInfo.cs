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
    public static class LyricInfo
    {
        /// <summary>
        /// Checks if requested item has a matching lyric file.
        /// </summary>
        /// <param name="lyricProvider">The current lyricProvider interface.</param>
        /// <param name="itemPath">Path of requested item.</param>
        /// <returns>True if item has a matching lyrics file.</returns>
        public static string? GetLyricFilePath(ILyricProvider lyricProvider, string itemPath)
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
    }
}
