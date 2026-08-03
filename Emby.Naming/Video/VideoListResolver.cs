using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.Naming.Common;
using Emby.Naming.TV;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.IO;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Resolves alternative versions and extras from list of video files.
    /// </summary>
    public partial class VideoListResolver
    {
        private static readonly StringComparer _numericOrdinalComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);

        private readonly NamingOptions _namingOptions;
        private readonly EpisodePathParser _episodePathParser;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoListResolver"/> class.
        /// </summary>
        /// <param name="namingOptions">The naming options.</param>
        public VideoListResolver(NamingOptions namingOptions)
        {
            _namingOptions = namingOptions;
            _episodePathParser = new EpisodePathParser(namingOptions);
        }

        [GeneratedRegex("[0-9]{2}[0-9]+[ip]", RegexOptions.IgnoreCase)]
        private static partial Regex ResolutionRegex();

        [GeneratedRegex(@"^\[([^]]*)\]")]
        private static partial Regex CheckMultiVersionRegex();

        /// <summary>
        /// Resolves alternative versions and extras from list of video files.
        /// </summary>
        /// <param name="videoInfos">List of related video files.</param>
        /// <param name="supportMultiVersion">Indication we should consider multi-versions of content.</param>
        /// <param name="parseName">Whether to parse the name or use the filename.</param>
        /// <param name="libraryRoot">Top-level folder for the containing library.</param>
        /// <param name="collectionType">The type of the containing collection, if known.</param>
        /// <returns>Returns enumerable of <see cref="VideoInfo"/> which groups files together when related.</returns>
        public IReadOnlyList<VideoInfo> Resolve(IReadOnlyList<VideoFileInfo> videoInfos, bool supportMultiVersion = true, bool parseName = true, string? libraryRoot = "", CollectionType? collectionType = null)
        {
            // Filter out all extras, otherwise they could cause stacks to not be resolved
            // See the unit test TestStackedWithTrailer
            var nonExtras = videoInfos
                .Where(i => i.ExtraType is null)
                .Select(i => new FileSystemMetadata { FullName = i.Path, IsDirectory = i.IsDirectory });

            var stackResult = StackResolver.Resolve(nonExtras, _namingOptions).ToList();

            var remainingFiles = new List<VideoFileInfo>();
            var standaloneMedia = new List<VideoFileInfo>();

            for (var i = 0; i < videoInfos.Count; i++)
            {
                var current = videoInfos[i];
                if (stackResult.Any(s => s.ContainsFile(current.Path, current.IsDirectory)))
                {
                    continue;
                }

                if (current.ExtraType is null)
                {
                    standaloneMedia.Add(current);
                }
                else
                {
                    remainingFiles.Add(current);
                }
            }

            var list = new List<VideoInfo>();

            foreach (var stack in stackResult)
            {
                var info = new VideoInfo(stack.Name)
                {
                    Files = stack.Files.Select(i => VideoResolver.Resolve(i, stack.IsDirectoryStack, _namingOptions, parseName, libraryRoot))
                        .OfType<VideoFileInfo>()
                        .ToList()
                };

                info.Year = info.Files[0].Year;
                list.Add(info);
            }

            foreach (var media in standaloneMedia)
            {
                var info = new VideoInfo(media.Name) { Files = new[] { media } };

                info.Year = info.Files[0].Year;
                list.Add(info);
            }

            if (supportMultiVersion)
            {
                list = collectionType is CollectionType.tvshows
                    ? GetEpisodesGroupedByVersion(list)
                    : GetVideosGroupedByVersion(list);
            }

            // Whatever files are left, just add them
            list.AddRange(remainingFiles.Select(i => new VideoInfo(i.Name)
            {
                Files = new[] { i },
                Year = i.Year,
                ExtraType = i.ExtraType
            }));

            return list;
        }

        private List<VideoInfo> GetVideosGroupedByVersion(List<VideoInfo> videos)
        {
            if (videos.Count == 0)
            {
                return videos;
            }

            var folderName = Path.GetFileName(Path.GetDirectoryName(videos[0].Files[0].Path.AsSpan()));

            if (folderName.Length <= 1 || !HaveSameYear(videos))
            {
                return videos;
            }

            // Cannot use Span inside local functions and delegates thus we cannot use LINQ here nor merge with the above [if]
            VideoInfo? primary = null;
            for (var i = 0; i < videos.Count; i++)
            {
                var video = videos[i];
                if (video.ExtraType is not null)
                {
                    continue;
                }

                if (!IsEligibleForMultiVersion(folderName, video.Files[0].FileNameWithoutExtension))
                {
                    return videos;
                }

                if (folderName.Equals(video.Files[0].FileNameWithoutExtension, StringComparison.Ordinal))
                {
                    primary = video;
                }
            }

            var organized = OrganizeAlternateVersions(videos, primary, folderName.ToString());

            return [organized];
        }

        private static bool HaveSameYear(IReadOnlyList<VideoInfo> videos)
        {
            if (videos.Count == 1)
            {
                return true;
            }

            var firstYear = videos[0].Year ?? -1;
            for (var i = 1; i < videos.Count; i++)
            {
                if ((videos[i].Year ?? -1) != firstYear)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsEligibleForMultiVersion(ReadOnlySpan<char> folderName, ReadOnlySpan<char> testFilename)
        {
            if (!testFilename.StartsWith(folderName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Remove the folder name before cleaning as we don't care about cleaning that part
            if (folderName.Length <= testFilename.Length)
            {
                testFilename = testFilename[folderName.Length..].Trim();
            }

            // There are no span overloads for regex unfortunately
            if (CleanStringParser.TryClean(testFilename.ToString(), _namingOptions.CleanStringRegexes, out var cleanName))
            {
                testFilename = cleanName.AsSpan().Trim();
            }

            // The CleanStringParser should have removed common keywords etc.
            return testFilename.IsEmpty
                   || testFilename[0] == '-'
                   || testFilename[0] == '_'
                   || testFilename[0] == '.'
                   || CheckMultiVersionRegex().IsMatch(testFilename);
        }

        private List<VideoInfo> GetEpisodesGroupedByVersion(List<VideoInfo> videos)
        {
            if (videos.Count < 2)
            {
                return videos;
            }

            var result = new List<VideoInfo>();
            var groups = new Dictionary<string, List<VideoInfo>>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < videos.Count; i++)
            {
                var video = videos[i];
                var episodeResult = _episodePathParser.Parse(video.Files[0].Path, false);
                string? key = null;
                if (episodeResult.Success)
                {
                    if (episodeResult.IsByDate
                        && episodeResult.Year.HasValue
                        && episodeResult.Month.HasValue
                        && episodeResult.Day.HasValue)
                    {
                        key = FormattableString.Invariant(
                            $"D{episodeResult.Year.Value}{episodeResult.Month.Value:D2}{episodeResult.Day.Value:D2}");
                    }
                    else if (episodeResult.EpisodeNumber.HasValue)
                    {
                        key = FormattableString.Invariant(
                            $"S{episodeResult.SeasonNumber ?? 0}E{episodeResult.EpisodeNumber.Value}");
                    }
                }

                if (key is null)
                {
                    result.Add(video);
                    continue;
                }

                if (!groups.TryGetValue(key, out var group))
                {
                    group = [];
                    groups[key] = group;
                }

                group.Add(video);
            }

            foreach (var group in groups.Values)
            {
                if (group.Count == 1)
                {
                    result.Add(group[0]);
                    continue;
                }

                result.Add(OrganizeAlternateVersions(group));
            }

            return result;
        }

        private static VideoInfo OrganizeAlternateVersions(
            List<VideoInfo> videos,
            VideoInfo? primaryOverride = null,
            string? nameOverride = null)
        {
            if (videos.Count > 1)
            {
                var groups = videos
                    .Select(x => (filename: x.Files[0].FileNameWithoutExtension.ToString(), value: x))
                    .Select(x => (x.filename, resolutionMatch: ResolutionRegex().Match(x.filename), x.value))
                    .GroupBy(x => x.resolutionMatch.Success)
                    .ToList();

                videos = [];

                foreach (var group in groups)
                {
                    if (group.Key)
                    {
                        videos.InsertRange(0, group
                            .OrderByDescending(x => x.resolutionMatch.Value, _numericOrdinalComparer)
                            .ThenBy(x => x.filename, _numericOrdinalComparer)
                            .Select(x => x.value));
                    }
                    else
                    {
                        videos.AddRange(group.OrderBy(x => x.filename, _numericOrdinalComparer).Select(x => x.value));
                    }
                }
            }

            // Prefer a stacked entry (more than one part) as primary
            var primary = primaryOverride
                ?? videos.FirstOrDefault(v => v.Files.Count > 1)
                ?? videos[0];
            videos.Remove(primary);

            primary.AlternateVersions = videos;

            if (nameOverride is not null)
            {
                primary.Name = nameOverride;
            }

            return primary;
        }
    }
}
