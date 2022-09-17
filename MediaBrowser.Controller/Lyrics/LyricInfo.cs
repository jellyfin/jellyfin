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
    /// Lyric helper methods.
    /// </summary>
    public static class LyricInfo
    {
        /// <summary>
        /// Gets matching lyric file for a requested item.
        /// </summary>
        /// <param name="lyricProvider">The lyricProvider interface to use.</param>
        /// <param name="itemPath">Path of requested item.</param>
        /// <returns>Lyric file path if passed lyric provider's supported media type is found; otherwise, null.</returns>
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
