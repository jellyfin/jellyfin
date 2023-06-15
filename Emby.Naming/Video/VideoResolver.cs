using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Emby.Naming.Common;
using Jellyfin.Extensions;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Resolves <see cref="VideoFileInfo"/> from file path.
    /// </summary>
    public static class VideoResolver
    {
        /// <summary>
        /// Resolves the directory.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="parseName">Whether to parse the name or use the filename.</param>
        /// <returns>VideoFileInfo.</returns>
        public static VideoFileInfo? ResolveDirectory(string? path, NamingOptions namingOptions, bool parseName = true)
        {
            return Resolve(path, true, namingOptions, parseName);
        }

        /// <summary>
        /// Resolves the file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <returns>VideoFileInfo.</returns>
        public static VideoFileInfo? ResolveFile(string? path, NamingOptions namingOptions)
        {
            return Resolve(path, false, namingOptions);
        }

        /// <summary>
        /// Resolves the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isDirectory">if set to <c>true</c> [is folder].</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="parseName">Whether or not the name should be parsed for info.</param>
        /// <returns>VideoFileInfo.</returns>
        /// <exception cref="ArgumentNullException"><c>path</c> is <c>null</c>.</exception>
        public static VideoFileInfo? Resolve(string? path, bool isDirectory, NamingOptions namingOptions, bool parseName = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            bool isStub = false;
            ReadOnlySpan<char> container = ReadOnlySpan<char>.Empty;
            string? stubType = null;

            if (!isDirectory)
            {
                var extension = Path.GetExtension(path.AsSpan());

                // Check supported extensions
                if (!namingOptions.VideoFileExtensions.Contains(extension, StringComparison.OrdinalIgnoreCase))
                {
                    // It's not supported. Check stub extensions
                    if (!StubResolver.TryResolveFile(path, namingOptions, out stubType))
                    {
                        return null;
                    }

                    isStub = true;
                }

                container = extension.TrimStart('.');
            }

            var format3DResult = Format3DParser.Parse(path, namingOptions);

            var extraResult = ExtraRuleResolver.GetExtraInfo(path, namingOptions);

            var name = Path.GetFileNameWithoutExtension(path);

            int? year = null;

            if (parseName)
            {
                var cleanDateTimeResult = CleanDateTime(name, namingOptions);
                name = cleanDateTimeResult.Name;
                year = cleanDateTimeResult.Year;

                if (TryCleanString(name, namingOptions, out var newName))
                {
                    name = newName;
                }
            }

            return new VideoFileInfo(
                path: path,
                container: container.IsEmpty ? null : container.ToString(),
                isStub: isStub,
                name: name,
                year: year,
                stubType: stubType,
                is3D: format3DResult.Is3D,
                format3D: format3DResult.Format3D,
                extraType: extraResult.ExtraType,
                isDirectory: isDirectory,
                extraRule: extraResult.Rule);
        }

        /// <summary>
        /// Determines if path is video file based on extension.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <returns>True if is video file.</returns>
        public static bool IsVideoFile(string path, NamingOptions namingOptions)
        {
            var extension = Path.GetExtension(path.AsSpan());
            return namingOptions.VideoFileExtensions.Contains(extension, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines if path is video file stub based on extension.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <returns>True if is video file stub.</returns>
        public static bool IsStubFile(string path, NamingOptions namingOptions)
        {
            var extension = Path.GetExtension(path.AsSpan());
            return namingOptions.StubFileExtensions.Contains(extension, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tries to clean name of clutter.
        /// </summary>
        /// <param name="name">Raw name.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="newName">Clean name.</param>
        /// <returns>True if cleaning of name was successful.</returns>
        public static bool TryCleanString([NotNullWhen(true)] string? name, NamingOptions namingOptions, out string newName)
        {
            return CleanStringParser.TryClean(name, namingOptions.CleanStringRegexes, out newName);
        }

        /// <summary>
        /// Tries to get name and year from raw name.
        /// </summary>
        /// <param name="name">Raw name.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <returns>Returns <see cref="CleanDateTimeResult"/> with name and optional year.</returns>
        public static CleanDateTimeResult CleanDateTime(string name, NamingOptions namingOptions)
        {
            return CleanDateTimeParser.Clean(name, namingOptions.CleanDateTimeRegexes);
        }
    }
}
