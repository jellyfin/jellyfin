using System;
using System.IO;
using Emby.Naming.Common;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Parses list of flags from filename based on delimiters.
    /// </summary>
    public class FlagParser
    {
        private readonly NamingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlagParser"/> class.
        /// </summary>
        /// <param name="options"><see cref="NamingOptions"/> object containing VideoFlagDelimiters.</param>
        public FlagParser(NamingOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Calls GetFlags function with _options.VideoFlagDelimiters parameter.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>List of found flags.</returns>
        public string[] GetFlags(string path)
        {
            return GetFlags(path, _options.VideoFlagDelimiters);
        }

        /// <summary>
        /// Parses flags from filename based on delimiters.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <param name="delimiters">Delimiters used to extract flags.</param>
        /// <returns>List of found flags.</returns>
        public string[] GetFlags(string path, char[] delimiters)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Array.Empty<string>();
            }

            // Note: the tags need be be surrounded be either a space ( ), hyphen -, dot . or underscore _.

            var file = Path.GetFileName(path);

            return file.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
