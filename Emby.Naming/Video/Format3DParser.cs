using System;
using Emby.Naming.Common;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Parse 3D format related flags.
    /// </summary>
    public static class Format3DParser
    {
        // Static default result to save on allocation costs.
        private static readonly Format3DResult _defaultResult = new (false, null);

        /// <summary>
        /// Parse 3D format related flags.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <returns>Returns <see cref="Format3DResult"/> object.</returns>
        public static Format3DResult Parse(string path, NamingOptions namingOptions)
        {
            int oldLen = namingOptions.VideoFlagDelimiters.Length;
            var delimiters = new char[oldLen + 1];
            namingOptions.VideoFlagDelimiters.CopyTo(delimiters, 0);
            delimiters[oldLen] = ' ';

            return Parse(path, delimiters, namingOptions);
        }

        private static Format3DResult Parse(ReadOnlySpan<char> path, char[] delimiters, NamingOptions namingOptions)
        {
            foreach (var rule in namingOptions.Format3DRules)
            {
                var result = Parse(path, rule, delimiters);

                if (result.Is3D)
                {
                    return result;
                }
            }

            return _defaultResult;
        }

        private static Format3DResult Parse(ReadOnlySpan<char> path, Format3DRule rule, char[] delimiters)
        {
            bool is3D = false;
            string? format3D = null;

            // If there's no preceding token we just consider it found
            var foundPrefix = string.IsNullOrEmpty(rule.PrecedingToken);
            while (path.Length > 0)
            {
                var index = path.IndexOfAny(delimiters);
                if (index == -1)
                {
                    index = path.Length - 1;
                }

                var currentSlice = path[..index];
                path = path[(index + 1)..];

                if (!foundPrefix)
                {
                    foundPrefix = currentSlice.Equals(rule.PrecedingToken, StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                is3D = foundPrefix && currentSlice.Equals(rule.Token, StringComparison.OrdinalIgnoreCase);

                if (is3D)
                {
                    format3D = rule.Token;
                    break;
                }
            }

            return is3D ? new Format3DResult(true, format3D) : _defaultResult;
        }
    }
}
