using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        public static bool TryClean([NotNullWhen(true)] string? name, IReadOnlyList<Regex> expressions, out ReadOnlySpan<char> newName)
        {
            if (string.IsNullOrEmpty(name))
            {
                newName = ReadOnlySpan<char>.Empty;
                return false;
            }

            // Iteratively apply the regexps to clean the string.
            bool cleaned = false;
            for (int i = 0; i < expressions.Count; i++)
            {
                if (TryClean(name, expressions[i], out newName))
                {
                    cleaned = true;
                    name = newName.ToString();
                }
            }

            newName = cleaned ? name.AsSpan() : ReadOnlySpan<char>.Empty;
            return cleaned;
        }

        private static bool TryClean(string name, Regex expression, out ReadOnlySpan<char> newName)
        {
            var match = expression.Match(name);
            int index = match.Index;
            if (match.Success)
            {
                var found = match.Groups.TryGetValue("cleaned", out var cleaned);
                if (!found || cleaned == null)
                {
                    newName = ReadOnlySpan<char>.Empty;
                    return false;
                }

                newName = name.AsSpan().Slice(cleaned.Index, cleaned.Length);
                return true;
            }

            newName = ReadOnlySpan<char>.Empty;
            return false;
        }
    }
}
