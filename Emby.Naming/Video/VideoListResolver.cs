using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.Naming.Common;
using Emby.Naming.TV;
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

        /// <summary>
        /// Resolves alternative versions and extras from list of video files.
        /// </summary>
        /// <param name="videoInfos">List of related video files.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="supportMultiVersion">Indication we should consider multi-versions of content.</param>
        /// <param name="parseName">Whether to parse the name or use the filename.</param>
        /// <param name="libraryRoot">Top-level folder for the containing library.</param>
        /// <param name="supportEpisodeGrouping">Whether to group episode files by parsed episode identity for multi-version support.</param>
        /// <returns>Returns enumerable of <see cref="VideoInfo"/> which groups files together when related.</returns>
        public static IReadOnlyList<VideoInfo> Resolve(IReadOnlyList<VideoFileInfo> videoInfos, NamingOptions namingOptions, bool supportMultiVersion = true, bool parseName = true, string? libraryRoot = "", bool supportEpisodeGrouping = false)
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
                list = supportEpisodeGrouping
                    ? GetEpisodesGroupedByVersion(list, namingOptions)
                    : GetVideosGroupedByVersion(list, namingOptions);
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

        /// <summary>
        /// Sorts videos by resolution (descending) then name (ascending), selects a primary version,
        /// and assigns the rest as alternate versions.
        /// </summary>
        private static List<VideoInfo> SortAndAssignVersions(List<VideoInfo> videos, VideoInfo? primary, string baseName)
        {
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
            list[0].Name = baseName;

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

            var (eligible, primary) = FindPrimaryVersion(videos, folderName, namingOptions);
            if (!eligible)
            {
                return videos;
            }

            return SortAndAssignVersions(videos, primary, folderName.ToString());
        }

        private static List<VideoInfo> GetEpisodesGroupedByVersion(List<VideoInfo> videos, NamingOptions namingOptions)
        {
            if (videos.Count == 0)
            {
                return videos;
            }

            var (episodeGroups, ungrouped) = GroupVideosByEpisode(videos, namingOptions);

            var filePath = videos[0].Files[0].Path.AsSpan();
            var folderName = Path.GetFileName(Path.GetDirectoryName(filePath));
            var grandparentFolderName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(filePath)));

            if (folderName.Length <= 1 || !HaveSameYear(videos))
            {
                return videos;
            }

            var result = new List<VideoInfo>();
            foreach (var group in episodeGroups.Values)
            {
                if (group.Count == 1)
                {
                    result.Add(group[0]);
                    continue;
                }

                var fileName = group[0].Files[0].FileNameWithoutExtension;
                if (!fileName.StartsWith(folderName, StringComparison.OrdinalIgnoreCase) && fileName.StartsWith(grandparentFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    folderName = grandparentFolderName;
                }

                if (folderName.Length <= 1 || !HaveSameYear(videos))
                {
                    return videos;
                }

                var (eligible, primary) = FindPrimaryVersion(group, folderName, namingOptions);
                result.AddRange(eligible
                    ? SortAndAssignVersions(group, primary, folderName.ToString())
                    : group);
            }

            result.AddRange(ungrouped);
            return result;
        }

        private static (Dictionary<(int? Season, int? Episode, int? EndingEpisode), List<VideoInfo>> Groups, List<VideoInfo> Ungrouped) GroupVideosByEpisode(
            List<VideoInfo> videos,
            NamingOptions namingOptions)
        {
            var episodeParser = new EpisodePathParser(namingOptions);
            var episodeGroups = new Dictionary<(int? Season, int? Episode, int? EndingEpisode), List<VideoInfo>>();
            var ungrouped = new List<VideoInfo>();

            foreach (var video in videos)
            {
                if (video.ExtraType is not null)
                {
                    ungrouped.Add(video);
                    continue;
                }

                var parseResult = episodeParser.Parse(video.Files[0].Path, false);
                if (parseResult is { Success: true, EpisodeNumber: not null })
                {
                    var key = (parseResult.SeasonNumber, parseResult.EpisodeNumber, parseResult.EndingEpisodeNumber);
                    if (!episodeGroups.TryGetValue(key, out var group))
                    {
                        group = new List<VideoInfo>();
                        episodeGroups[key] = group;
                    }

                    group.Add(video);
                }
                else
                {
                    ungrouped.Add(video);
                }
            }

            return (episodeGroups, ungrouped);
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

        private static (bool Eligible, VideoInfo? Primary) FindPrimaryVersion(
            List<VideoInfo> videos,
            ReadOnlySpan<char> baseName,
            NamingOptions namingOptions)
        {
            VideoInfo? primary = null;
            for (var i = 0; i < videos.Count; i++)
            {
                var video = videos[i];
                if (video.ExtraType is not null)
                {
                    continue;
                }

                if (!IsEligibleForMultiVersion(baseName, video.Files[0].FileNameWithoutExtension, namingOptions))
                {
                    return (false, null);
                }

                if (primary is null)
                {
                    var fileName = video.Files[0].FileNameWithoutExtension;
                    if (baseName.Equals(fileName, StringComparison.Ordinal)
                        || (fileName.StartsWith(baseName, StringComparison.OrdinalIgnoreCase)
                            && Regex.IsMatch(fileName[baseName.Length..].Trim().ToString(), @"^[\s\-\.]*S\d+E\d+(-E\d+)*[\s\-\.]*$", RegexOptions.None, TimeSpan.FromSeconds(1))))
                    {
                        primary = video;
                    }
                }
            }

            return (true, primary);
        }

        private static bool IsEligibleForMultiVersion(ReadOnlySpan<char> folderName, ReadOnlySpan<char> testFilename, NamingOptions namingOptions)
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

            // Strip episode identifiers (S##E##, S##E##-E##, etc.) and surrounding separators
            var episodeMatch = Regex.Match(testFilename.ToString(), @"^[\s\-\.]*S\d+E\d+(-E\d+)*", RegexOptions.None, TimeSpan.FromSeconds(1));
            if (episodeMatch.Success)
            {
                testFilename = testFilename[episodeMatch.Length..];
            }

            // There are no span overloads for regex unfortunately
            if (CleanStringParser.TryClean(testFilename.ToString(), namingOptions.CleanStringRegexes, out var cleanName))
            {
                testFilename = cleanName.AsSpan().Trim();
            }

            // The CleanStringParser should have removed common keywords etc.
            return testFilename.IsEmpty
                   || testFilename[0] == '-'
                   || CheckMultiVersionRegex().IsMatch(testFilename);
        }
    }
}
