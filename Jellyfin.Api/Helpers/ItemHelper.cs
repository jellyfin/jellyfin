using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfin.Api.Models.UserDtos;
using LrcParser.Model;
using LrcParser.Parser;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Item helper.
    /// </summary>
    public static class ItemHelper
    {
        /// <summary>
        /// Opens lyrics file, converts to a List of Lyrics, and returns it.
        /// </summary>
        /// <param name="item">Requested Item.</param>
        /// <returns>Collection of Lyrics.</returns>
        internal static object? GetLyricData(BaseItem item)
        {
            List<ILyricsProvider> providerList = new List<ILyricsProvider>();

            // Find all classes that implement ILyricsProvider Interface
            var foundLyricProviders = System.Reflection.Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type => typeof(ILyricsProvider).IsAssignableFrom(type) && !type.IsInterface);

            if (!foundLyricProviders.Any())
            {
                return null;
            }

            foreach (var provider in foundLyricProviders)
            {
                ILyricsProvider? newProvider = Activator.CreateInstance(provider) as ILyricsProvider;
                if (newProvider is not null)
                {
                    newProvider.Process(item);
                    if (newProvider.HasData)
                    {
                        return newProvider.Data;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if requested item has a matching lyric file.
        /// </summary>
        /// <param name="itemPath">Path of requested item.</param>
        /// <returns>True if item has a matching lyrics file.</returns>
        public static string? GetLyricFilePath(string itemPath)
        {
            List<string> supportedLyricFileExtensions = new List<string>();

            // Find all classes that implement ILyricsProvider Interface
            var foundLyricProviders = System.Reflection.Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type => typeof(ILyricsProvider).IsAssignableFrom(type) && !type.IsInterface);

            if (!foundLyricProviders.Any())
            {
                return null;
            }

            // Iterate over all found lyric providers
            foreach (var provider in foundLyricProviders)
            {
                ILyricsProvider? foundProvider = Activator.CreateInstance(provider) as ILyricsProvider;
                if (foundProvider?.FileExtensions is null)
                {
                    continue;
                }

                if (foundProvider.FileExtensions.Any())
                {
                    foreach (string lyricFileExtension in foundProvider.FileExtensions)
                    {
                        string lyricFilePath = @Path.ChangeExtension(itemPath, lyricFileExtension);
                        if (System.IO.File.Exists(lyricFilePath))
                        {
                            return lyricFilePath;
                        }
                    }
                }
            }

            return null;
        }


        /// <summary>
        /// Checks if requested item has a matching local lyric file.
        /// </summary>
        /// <param name="itemPath">Path of requested item.</param>
        /// <returns>True if item has a matching lyrics file; otherwise false.</returns>
        public static bool HasLyricFile(string itemPath)
        {
            string? lyricFilePath = GetLyricFilePath(itemPath);
            return !string.IsNullOrEmpty(lyricFilePath);
        }
    }
}
