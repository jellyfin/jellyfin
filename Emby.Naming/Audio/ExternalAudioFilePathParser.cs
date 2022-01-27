using System;
using System.IO;
using System.Linq;
using Emby.Naming.Common;
using Jellyfin.Extensions;

namespace Emby.Naming.Audio
{
    /// <summary>
    /// External Audio Parser class.
    /// </summary>
    public class ExternalAudioFilePathParser
    {
        private readonly NamingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalAudioFilePathParser"/> class.
        /// </summary>
        /// <param name="options"><see cref="NamingOptions"/> object containing AudioFileExtensions, ExternalAudioDefaultFlags, ExternalAudioForcedFlags and ExternalAudioFlagDelimiters.</param>
        public ExternalAudioFilePathParser(NamingOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Parse file to determine if it is a ExternalAudio and <see cref="ExternalAudioFileInfo"/>.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>Returns null or <see cref="ExternalAudioFileInfo"/> object if parsing is successful.</returns>
        public ExternalAudioFileInfo? ParseFile(string path)
        {
            if (path.Length == 0)
            {
                return null;
            }

            var extension = Path.GetExtension(path);
            if (!_options.AudioFileExtensions.Contains(extension, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var flags = GetFileFlags(path);
            var info = new ExternalAudioFileInfo(
                path,
                _options.ExternalAudioDefaultFlags.Any(i => flags.Contains(i, StringComparison.OrdinalIgnoreCase)),
                _options.ExternalAudioForcedFlags.Any(i => flags.Contains(i, StringComparison.OrdinalIgnoreCase)));

            return info;
        }

        private string[] GetFileFlags(string path)
        {
            var file = Path.GetFileNameWithoutExtension(path);

            return file.Split(_options.ExternalAudioFlagDelimiters, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
