using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Emby.Naming.Video
{
    /// <summary>
    /// <see href="http://kodi.wiki/view/Advancedsettings.xml#video" />.
    /// </summary>
    public static class CleanStringParser
    {
        /// <summary>
        /// Attempts to extract clean name with regular expressions.
        /// </summary>
        /// <param name="name">Name of file.</param>
        /// <param name="expressions">List of regex to parse name and year from.</param>
        /// <param name="newName">Parsing result string.</param>
        /// <returns>True if parsing was successful.</returns>
        public static bool TryClean(string name, IReadOnlyList<Regex> expressions, out ReadOnlySpan<char> newName)
        {
            var len = expressions.Count;
            for (int i = 0; i < len; i++)
            {
                if (TryClean(name, expressions[i], out newName))
                {
                    return true;
                }
            }

            newName = ReadOnlySpan<char>.Empty;
            return false;
        }

        private static bool TryClean(string name, Regex expression, out ReadOnlySpan<char> newName)
        {
            if (string.IsNullOrEmpty(name))
            {
                newName = ReadOnlySpan<char>.Empty;
                return false;
            }

            var match = expression.Match(name);
            int index = match.Index;
            if (match.Success && index != 0)
            {
                newName = name.AsSpan().Slice(0, match.Index);
                return true;
            }

            newName = ReadOnlySpan<char>.Empty;
            return false;
        }
    }
}
