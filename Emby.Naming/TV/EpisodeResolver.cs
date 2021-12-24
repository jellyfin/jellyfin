using System;
using System.IO;
using Emby.Naming.Common;
using Emby.Naming.Video;
using Jellyfin.Extensions;

namespace Emby.Naming.TV
{
    /// <summary>
    /// Used to resolve information about episode from path.
    /// </summary>
    public class EpisodeResolver
    {
        private readonly NamingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeResolver"/> class.
        /// </summary>
        /// <param name="options"><see cref="NamingOptions"/> object containing VideoFileExtensions and passed to <see cref="StubResolver"/>, <see cref="Format3DParser"/> and <see cref="EpisodePathParser"/>.</param>
        public EpisodeResolver(NamingOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Resolve information about episode from path.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <param name="isDirectory">Is path for a directory or file.</param>
        /// <param name="isNamed">Do we want to use IsNamed expressions.</param>
        /// <param name="isOptimistic">Do we want to use Optimistic expressions.</param>
        /// <param name="supportsAbsoluteNumbers">Do we want to use expressions supporting absolute episode numbers.</param>
        /// <param name="fillExtendedInfo">Should we attempt to retrieve extended information.</param>
        /// <returns>Returns null or <see cref="EpisodeInfo"/> object if successful.</returns>
        public EpisodeInfo? Resolve(
            string path,
            bool isDirectory,
            bool? isNamed = null,
            bool? isOptimistic = null,
            bool? supportsAbsoluteNumbers = null,
            bool fillExtendedInfo = true)
        {
            bool isStub = false;
            string? container = null;
            string? stubType = null;

            if (!isDirectory)
            {
                var extension = Path.GetExtension(path);
                // Check supported extensions
                if (!_options.VideoFileExtensions.Contains(extension, StringComparison.OrdinalIgnoreCase))
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

            var format3DResult = Format3DParser.Parse(path, _options);

            var parsingResult = new EpisodePathParser(_options)
                .Parse(path, isDirectory, isNamed, isOptimistic, supportsAbsoluteNumbers, fillExtendedInfo);

            if (!parsingResult.Success && !isStub)
            {
                return null;
            }

            return new EpisodeInfo(path)
            {
                Container = container,
                IsStub = isStub,
                EndingEpisodeNumber = parsingResult.EndingEpisodeNumber,
                EpisodeNumber = parsingResult.EpisodeNumber,
                SeasonNumber = parsingResult.SeasonNumber,
                SeriesName = parsingResult.SeriesName,
                StubType = stubType,
                Is3D = format3DResult.Is3D,
                Format3D = format3DResult.Format3D,
                IsByDate = parsingResult.IsByDate,
                Day = parsingResult.Day,
                Month = parsingResult.Month,
                Year = parsingResult.Year
            };
        }
    }
}
