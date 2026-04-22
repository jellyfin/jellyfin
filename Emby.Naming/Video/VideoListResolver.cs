using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.Naming.Common;
using Jellyfin.Extensions;
using MediaBrowser.Model.IO;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Resolves alternative versions and extras from list of video files.
    /// </summary>
    public static partial class VideoListResolver
    {
        [GeneratedRegex("[0-9]{2}[0-9]+[ip]", RegexOptions.IgnoreCase)]
        private static partial Regex ResolutionRegex();

        [GeneratedRegex(@"^\[([^]]*)\]")]
        private static partial Regex CheckMultiVersionRegex();

        [GeneratedRegex(@"[\[\(\{](?:tmdbid|tmdb|imdbid|imdb|tvdbid|tvdb)[=-][^\]\)\}]+[\]\)\}]", RegexOptions.IgnoreCase)]
        private static partial Regex ProviderIdTokenRegex();

        /// <summary>
        /// Resolves alternative versions and extras from list of video files.
        /// </summary>
        /// <param name="videoInfos">List of related video files.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="supportMultiVersion">Indication we should consider multi-versions of content.</param>
        /// <param name="parseName">Whether to parse the name or use the filename.</param>
        /// <param name="libraryRoot">Top-level folder for the containing library.</param>
        /// <returns>Returns enumerable of <see cref="VideoInfo"/> which groups files together when related.</returns>
        public static IReadOnlyList<VideoInfo> Resolve(IReadOnlyList<VideoFileInfo> videoInfos, NamingOptions namingOptions, bool supportMultiVersion = true, bool parseName = true, string? libraryRoot = "")
        {
            // Filter out all extras, otherwise they could cause stacks to not be resolved
            // See the unit test TestStackedWithTrailer
            var nonExtras = videoInfos
                .Where(i => i.ExtraType is null)
                .Select(i => new FileSystemMetadata { FullName = i.Path, IsDirectory = i.IsDirectory });

            var stackResult = StackResolver.Resolve(nonExtras, namingOptions).ToList();

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
                    Files = stack.Files.Select(i => VideoResolver.Resolve(i, stack.IsDirectoryStack, namingOptions, parseName, libraryRoot))
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
                list = GetVideosGroupedByVersion(list, namingOptions);
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

        private static List<VideoInfo> GetVideosGroupedByVersion(List<VideoInfo> videos, NamingOptions namingOptions)
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

                if (!IsEligibleForMultiVersion(folderName, video.Files[0].FileNameWithoutExtension, namingOptions))
                {
                    return videos;
                }

                if (folderName.Equals(video.Files[0].FileNameWithoutExtension, StringComparison.Ordinal))
                {
                    primary = video;
                }
            }

            if (videos.Count > 1)
            {
                var groups = videos
                    .Select(x => (filename: x.Files[0].FileNameWithoutExtension.ToString(), value: x))
                    .Select(x => (x.filename, resolutionMatch: ResolutionRegex().Match(x.filename), x.value))
                    .GroupBy(x => x.resolutionMatch.Success)
                    .ToList();

                videos.Clear();

                StringComparer comparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);
                foreach (var group in groups)
                {
                    if (group.Key)
                    {
                        videos.InsertRange(0, group
                            .OrderByDescending(x => x.resolutionMatch.Value, comparer)
                            .ThenBy(x => x.filename, comparer)
                            .Select(x => x.value));
                    }
                    else
                    {
                        videos.AddRange(group.OrderBy(x => x.filename, comparer).Select(x => x.value));
                    }
                }
            }

            primary ??= videos[0];
            videos.Remove(primary);

            var list = new List<VideoInfo>
            {
                primary
            };

            list[0].AlternateVersions = videos.Select(x => x.Files[0]).ToArray();
            list[0].Name = folderName.ToString();

            return list;
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

        private static bool IsEligibleForMultiVersion(ReadOnlySpan<char> folderName, ReadOnlySpan<char> testFilename, NamingOptions namingOptions)
        {
            var rawFolderName = StripProviderIdTokens(folderName);
            var rawTestFilename = StripProviderIdTokens(testFilename);

            if (!rawTestFilename.StartsWith(rawFolderName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var rawSuffix = rawTestFilename.Length > rawFolderName.Length
                ? rawTestFilename[rawFolderName.Length..].Trim()
                : string.Empty;

            if (HasUnmatchedOpeningBracket(rawSuffix))
            {
                return false;
            }

            var normalizedFolderName = NormalizeMultiVersionName(folderName, namingOptions);
            var normalizedTestFilename = NormalizeMultiVersionName(testFilename, namingOptions);

            if (!normalizedTestFilename.StartsWith(normalizedFolderName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Remove the folder name before cleaning as we don't care about cleaning that part
            if (normalizedFolderName.Length <= normalizedTestFilename.Length)
            {
                normalizedTestFilename = normalizedTestFilename[normalizedFolderName.Length..].Trim().ToString();
            }

            // The NormalizeMultiVersionName should have removed common keywords etc.
            return normalizedTestFilename.Length == 0
                   || normalizedTestFilename[0] == '-'
                   || normalizedTestFilename[0] == '_'
                   || normalizedTestFilename[0] == '.'
                   || CheckMultiVersionRegex().IsMatch(normalizedTestFilename);
        }

        private static string NormalizeMultiVersionName(ReadOnlySpan<char> name, NamingOptions namingOptions)
        {
            var normalizedName = StripProviderIdTokens(name);

            normalizedName = VideoResolver.CleanDateTime(normalizedName, namingOptions).Name;

            if (CleanStringParser.TryClean(normalizedName, namingOptions.CleanStringRegexes, out var cleanName))
            {
                normalizedName = cleanName;
            }

            return normalizedName.Trim();
        }

        private static string StripProviderIdTokens(ReadOnlySpan<char> name)
            => ProviderIdTokenRegex().Replace(name.ToString(), string.Empty).Trim();

        private static bool HasUnmatchedOpeningBracket(string value)
        {
            int square = 0, round = 0, curly = 0;

            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '[':
                        square++;
                        break;
                    case ']':
                        square = square > 0 ? square - 1 : square;
                        break;
                    case '(':
                        round++;
                        break;
                    case ')':
                        round = round > 0 ? round - 1 : round;
                        break;
                    case '{':
                        curly++;
                        break;
                    case '}':
                        curly = curly > 0 ? curly - 1 : curly;
                        break;
                }
            }

            return square > 0 || round > 0 || curly > 0;
        }
    }
}
