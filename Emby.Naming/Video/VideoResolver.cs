using Emby.Naming.Common;
using System;
using System.IO;
using System.Linq;

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
        public VideoFileInfo ResolveDirectory(string path)
        {
            return Resolve(path, true);
        }

        /// <summary>
        /// Resolves the file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>VideoFileInfo.</returns>
        public VideoFileInfo ResolveFile(string path)
        {
            return Resolve(path, false);
        }

        /// <summary>
        /// Resolves the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="IsDirectory">if set to <c>true</c> [is folder].</param>
        /// <returns>VideoFileInfo.</returns>
        /// <exception cref="System.ArgumentNullException">path</exception>
        public VideoFileInfo Resolve(string path, bool IsDirectory, bool parseName = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var isStub = false;
            string container = null;
            string stubType = null;

            if (!IsDirectory)
            {
                var extension = Path.GetExtension(path) ?? string.Empty;
                // Check supported extensions
                if (!_options.VideoFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    var stubResult = new StubResolver(_options).ResolveFile(path);

                    isStub = stubResult.IsStub;

                    // It's not supported. Check stub extensions
                    if (!isStub)
                    {
                        return null;
                    }

                    stubType = stubResult.StubType;
                }

                container = extension.TrimStart('.');
            }

            var flags = new FlagParser(_options).GetFlags(path);
            var format3DResult = new Format3DParser(_options).Parse(flags);

            var extraResult = new ExtraResolver(_options).GetExtraInfo(path);

            var name = !IsDirectory
                ? Path.GetFileNameWithoutExtension(path)
                : Path.GetFileName(path);

            int? year = null;

            if (parseName)
            {
                var cleanDateTimeResult = CleanDateTime(name);

                if (string.IsNullOrEmpty(extraResult.ExtraType))
                {
                    name = cleanDateTimeResult.Name;
                    name = CleanString(name).Name;
                }

                year = cleanDateTimeResult.Year;
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
                IsDirectory = IsDirectory,
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

        public CleanStringResult CleanString(string name)
        {
            return new CleanStringParser().Clean(name, _options.CleanStringRegexes);
        }

        public CleanDateTimeResult CleanDateTime(string name)
        {
            return new CleanDateTimeParser(_options).Clean(name);
        }
    }
}
