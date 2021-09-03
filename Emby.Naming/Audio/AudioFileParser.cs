using System;
using System.IO;
using Emby.Naming.Common;
using Jellyfin.Extensions;

namespace Emby.Naming.Audio
{
    /// <summary>
    /// Static helper class to determine if file at path is audio file.
    /// </summary>
    public static class AudioFileParser
    {
        /// <summary>
        /// Static helper method to determine if file at path is audio file.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <param name="options"><see cref="NamingOptions"/> containing AudioFileExtensions.</param>
        /// <returns>True if file at path is audio file.</returns>
        public static bool IsAudioFile(string path, NamingOptions options)
        {
            var extension = Path.GetExtension(path.AsSpan());
            return options.AudioFileExtensions.Contains(extension, StringComparison.OrdinalIgnoreCase);
        }
    }
}
