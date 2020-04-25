#pragma warning disable CS1591
#nullable enable

using System;
using System.IO;
using System.Linq;
using Emby.Naming.Common;

namespace Emby.Naming.Video
{
    public class VideoResolver
    {
        private readonly NamingOptions _options;

        public VideoResolver(NamingOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Resolves the directory.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>VideoFileInfo.</returns>
        public VideoFileInfo? ResolveDirectory(string path)
        {
            return Resolve(path, true);
        }

        /// <summary>
        /// Resolves the file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>VideoFileInfo.</returns>
        public VideoFileInfo? ResolveFile(string path)
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
        public VideoFileInfo? Resolve(string path, bool isDirectory, bool parseName = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
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

            return new VideoFileInfo
            {
                Path = path,
                Container = container,
                IsStub = isStub,
                Name = name,
                Year = year,
                StubType = stubType,
                Is3D = format3DResult.Is3D,
                Format3D = format3DResult.Format3D,
                ExtraType = extraResult.ExtraType,
                IsDirectory = isDirectory,
                ExtraRule = extraResult.Rule
            };
        }

        public bool IsVideoFile(string path)
        {
            var extension = Path.GetExtension(path) ?? string.Empty;
            return _options.VideoFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsStubFile(string path)
        {
            var extension = Path.GetExtension(path) ?? string.Empty;
            return _options.StubFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        public bool TryCleanString(string name, out ReadOnlySpan<char> newName)
        {
            return CleanStringParser.TryClean(name, _options.CleanStringRegexes, out newName);
        }

        public CleanDateTimeResult CleanDateTime(string name)
        {
            return CleanDateTimeParser.Clean(name, _options.CleanDateTimeRegexes);
        }
    }
}
