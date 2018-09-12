using Emby.Naming.Common;
using Emby.Naming.Video;
using System;
using System.IO;
using System.Linq;

namespace Emby.Naming.TV
{
    public class EpisodeResolver
    {
        private readonly NamingOptions _options;

        public EpisodeResolver(NamingOptions options)
        {
            _options = options;
        }

        public EpisodeInfo Resolve(string path, bool IsDirectory, bool? isNamed = null, bool? isOptimistic = null, bool? supportsAbsoluteNumbers = null, bool fillExtendedInfo = true)
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

            var parsingResult = new EpisodePathParser(_options)
                .Parse(path, IsDirectory, isNamed, isOptimistic, supportsAbsoluteNumbers, fillExtendedInfo);
            
            return new EpisodeInfo
            {
                Path = path,
                Container = container,
                IsStub = isStub,
                EndingEpsiodeNumber = parsingResult.EndingEpsiodeNumber,
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
