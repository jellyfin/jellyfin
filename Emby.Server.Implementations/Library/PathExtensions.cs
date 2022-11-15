using System;
using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Common.Providers;

namespace Emby.Server.Implementations.Library
{
    /// <summary>
    /// Class providing extension methods for working with paths.
    /// </summary>
    public static class PathExtensions
    {
        /// <summary>
        /// Gets the attribute value.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <param name="attribute">The attrib.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentException"><paramref name="str" /> or <paramref name="attribute" /> is empty.</exception>
        public static string? GetAttributeValue(this ReadOnlySpan<char> str, ReadOnlySpan<char> attribute)
        {
            if (str.Length == 0)
            {
                throw new ArgumentException("String can't be empty.", nameof(str));
            }

            if (attribute.Length == 0)
            {
                throw new ArgumentException("String can't be empty.", nameof(attribute));
            }

            var attributeIndex = str.IndexOf(attribute, StringComparison.OrdinalIgnoreCase);

            // Must be at least 3 characters after the attribute =, ], any character.
            var maxIndex = str.Length - attribute.Length - 3;
            while (attributeIndex > -1 && attributeIndex < maxIndex)
            {
                var attributeEnd = attributeIndex + attribute.Length;
                if (attributeIndex > 0
                    && str[attributeIndex - 1] == '['
                    && (str[attributeEnd] == '=' || str[attributeEnd] == '-'))
                {
                    var closingIndex = str[attributeEnd..].IndexOf(']');
                    // Must be at least 1 character before the closing bracket.
                    if (closingIndex > 1)
                    {
                        return str[(attributeEnd + 1)..(attributeEnd + closingIndex)].Trim().ToString();
                    }
                }

                str = str[attributeEnd..];
                attributeIndex = str.IndexOf(attribute, StringComparison.OrdinalIgnoreCase);
            }

            // for imdbid we also accept pattern matching
            if (attribute.Equals("imdbid", StringComparison.OrdinalIgnoreCase))
            {
                var match = ProviderIdParsers.TryFindImdbId(str, out var imdbId);
                return match ? imdbId.ToString() : null;
            }

            return null;
        }

        /// <summary>
        /// Replaces a sub path with another sub path and normalizes the final path.
        /// </summary>
        /// <param name="path">The original path.</param>
        /// <param name="subPath">The original sub path.</param>
        /// <param name="newSubPath">The new sub path.</param>
        /// <param name="newPath">The result of the sub path replacement.</param>
        /// <returns>The path after replacing the sub path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path" />, <paramref name="newSubPath" /> or <paramref name="newSubPath" /> is empty.</exception>
        public static bool TryReplaceSubPath(
            [NotNullWhen(true)] this string? path,
            [NotNullWhen(true)] string? subPath,
            [NotNullWhen(true)] string? newSubPath,
            [NotNullWhen(true)] out string? newPath)
        {
            newPath = null;

            if (string.IsNullOrEmpty(path)
                || string.IsNullOrEmpty(subPath)
                || string.IsNullOrEmpty(newSubPath)
                || subPath.Length > path.Length)
            {
                return false;
            }

            char oldDirectorySeparatorChar;
            char newDirectorySeparatorChar;
            // True normalization is still not possible https://github.com/dotnet/runtime/issues/2162
            // The reasoning behind this is that a forward slash likely means it's a Linux path and
            // so the whole path should be normalized to use / and vice versa for Windows (although Windows doesn't care much).
            if (newSubPath.Contains('/', StringComparison.Ordinal))
            {
                oldDirectorySeparatorChar = '\\';
                newDirectorySeparatorChar = '/';
            }
            else
            {
                oldDirectorySeparatorChar = '/';
                newDirectorySeparatorChar = '\\';
            }

            path = path.Replace(oldDirectorySeparatorChar, newDirectorySeparatorChar);
            subPath = subPath.Replace(oldDirectorySeparatorChar, newDirectorySeparatorChar);

            // We have to ensure that the sub path ends with a directory separator otherwise we'll get weird results
            // when the sub path matches a similar but in-complete subpath
            var oldSubPathEndsWithSeparator = subPath[^1] == newDirectorySeparatorChar;
            if (!path.StartsWith(subPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (path.Length > subPath.Length
                && !oldSubPathEndsWithSeparator
                && path[subPath.Length] != newDirectorySeparatorChar)
            {
                return false;
            }

            var newSubPathTrimmed = newSubPath.AsSpan().TrimEnd(newDirectorySeparatorChar);
            // Ensure that the path with the old subpath removed starts with a leading dir separator
            int idx = oldSubPathEndsWithSeparator ? subPath.Length - 1 : subPath.Length;
            newPath = string.Concat(newSubPathTrimmed, path.AsSpan(idx));

            return true;
        }
    }
}
