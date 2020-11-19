using System;
using System.IO;
using System.Linq;
using Emby.Naming.Common;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Resolves <see cref="VideoFileInfo"/> from file path.
    /// </summary>
    public class VideoResolver
    {
        private readonly NamingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoResolver"/> class.
        /// </summary>
        /// <param name="options"><see cref="NamingOptions"/> object containing VideoFileExtensions, StubFileExtensions, CleanStringRegexes and CleanDateTimeRegexes
        /// and passes options in <see cref="StubResolver"/>, <see cref="FlagParser"/>, <see cref="Format3DParser"/> and <see cref="ExtraResolver"/>.</param>
        public VideoResolver(NamingOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Resolves the directory.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>VideoFileInfo.</returns>
        public VideoFileInfo? ResolveDirectory(string? path)
        {
            return Resolve(path, true);
        }

        /// <summary>
        /// Resolves the file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>VideoFileInfo.</returns>
        public VideoFileInfo? ResolveFile(string? path)
        {
            return Resolve(path, false);
        }

        /// <summary>
        /// Resolves the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isDirectory">if set to <c>true</c> [is folder].</param>
        /// <param name="parseName">Whether or not the name should be parsed for info.</param>
        /// <returns>VideoFileInfo.</returns>
        /// <exception cref="ArgumentNullException"><c>path</c> is <c>null</c>.</exception>
        public VideoFileInfo? Resolve(string? path, bool isDirectory, bool parseName = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            bool isStub = false;
            string? container = null;
            string? stubType = null;

            if (!isDirectory)
            {
                var extension = Path.GetExtension(path);

                // Check supported extensions
                if (!_options.VideoFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    // It's not supported. Check stub extensions
                    if (!StubResolver.TryResolveFile(path, _options, out stubType))
                    {
                        return null;
                    }

                    isStub = true;
                }

                container = extension.TrimStart('.');
            }

            var flags = new FlagParser(_options).GetFlags(path);
            var format3DResult = new Format3DParser(_options).Parse(flags);

            var extraResult = new ExtraResolver(_options).GetExtraInfo(path);

            var name = isDirectory
                ? Path.GetFileName(path)
                : Path.GetFileNameWithoutExtension(path);

            int? year = null;

            if (parseName)
            {
                var cleanDateTimeResult = CleanDateTime(name);
                name = cleanDateTimeResult.Name;
                year = cleanDateTimeResult.Year;

                if (extraResult.ExtraType == null
                    && TryCleanString(name, out ReadOnlySpan<char> newName))
                {
                    name = newName.ToString();
                }
            }

            return new VideoFileInfo(
                path: path,
                container: container,
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
        /// <returns>True if is video file.</returns>
        public bool IsVideoFile(string path)
        {
            var extension = Path.GetExtension(path) ?? string.Empty;
            return _options.VideoFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines if path is video file stub based on extension.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>True if is video file stub.</returns>
        public bool IsStubFile(string path)
        {
            var extension = Path.GetExtension(path) ?? string.Empty;
            return _options.StubFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tries to clean name of clutter.
        /// </summary>
        /// <param name="name">Raw name.</param>
        /// <param name="newName">Clean name.</param>
        /// <returns>True if cleaning of name was successful.</returns>
        public bool TryCleanString(string name, out ReadOnlySpan<char> newName)
        {
            return CleanStringParser.TryClean(name, _options.CleanStringRegexes, out newName);
        }

        /// <summary>
        /// Tries to get name and year from raw name.
        /// </summary>
        /// <param name="name">Raw name.</param>
        /// <returns>Returns <see cref="CleanDateTimeResult"/> with name and optional year.</returns>
        public CleanDateTimeResult CleanDateTime(string name)
        {
            return CleanDateTimeParser.Clean(name, _options.CleanDateTimeRegexes);
        }
    }
}
