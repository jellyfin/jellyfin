using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.Naming.Common;
using Emby.Naming.TV;
using Jellyfin.Extensions;
using MediaBrowser.Model.Entities;
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
        /// <param name="collectionType">Collection type of videos being resolved.</param>
        /// <param name="supportMultiVersion">Indication we should consider multi-versions of content.</param>
        /// <param name="parseName">Whether to parse the name or use the filename.</param>
        /// <returns>Returns enumerable of <see cref="VideoInfo"/> which groups files together when related.</returns>
        public static IReadOnlyList<VideoInfo> Resolve(
            IReadOnlyList<VideoFileInfo> videoInfos,
            NamingOptions namingOptions,
            string collectionType,
            bool supportMultiVersion = true,
            bool parseName = true)
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
                    Files = stack.Files.Select(i => VideoResolver.Resolve(i, stack.IsDirectoryStack, namingOptions, parseName))
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
                if (info.Year is null)
                {
                    // parse name for year info. Episodes don't get parsed up to this point for year info
                    var info2 = VideoResolver.Resolve(media.Path, media.IsDirectory, namingOptions, parseName);
                    info.Year = info2?.Year;
                }

                list.Add(info);
            }

            if (supportMultiVersion)
            {
                list = GetVideosGroupedByVersion(list, namingOptions, collectionType);
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

        private static List<VideoInfo> GetVideosGroupedByVersion(List<VideoInfo> videos, NamingOptions namingOptions, string collectionType)
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

            var mergeable = new List<VideoInfo>();
            var notMergeable = new List<VideoInfo>();
            // Cannot use Span inside local functions and delegates thus we cannot use LINQ here nor merge with the above [if]
            VideoInfo? primary = null;
            for (var i = 0; i < videos.Count; i++)
            {
                var video = videos[i];
                if (video.ExtraType is not null)
                {
                    continue;
                }

                // don't merge stacked episodes
                if (video.Files.Count() == 1 && IsEligibleForMultiVersion(folderName, video.Files[0].Path, namingOptions, collectionType))
                {
                    mergeable.Add(video);
                }
                else
                {
                    notMergeable.Add(video);
                }

                if (folderName.Equals(video.Files[0].FileNameWithoutExtension, StringComparison.Ordinal))
                {
                    primary = video;
                }
            }

            var list = new List<VideoInfo>();
            if (collectionType.Equals(CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
            {
                var groupedList = mergeable.GroupBy(x => EpisodeGrouper(x.Files[0].Path, namingOptions, collectionType));
                foreach (var grouping in groupedList)
                {
                    list.Add(OrganizeAlternateVersions(grouping.ToList(), grouping.Key.AsSpan(), primary));
                }
            }
            else if (mergeable.Count() > 0)
            {
                list.Add(OrganizeAlternateVersions(mergeable, folderName, primary));
            }

            // add non mergeables back in
            list.AddRange(notMergeable);
            list.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

            return list;
        }

        private static VideoInfo OrganizeAlternateVersions(List<VideoInfo> grouping, ReadOnlySpan<char> name, VideoInfo? primary)
        {
            VideoInfo? groupPrimary = null;
            if (primary is not null && grouping.Contains(primary))
            {
                groupPrimary = primary;
            }

            var alternateVersions = new List<VideoInfo>();
            if (grouping.Count() > 1)
            {
                // groups resolution based into one, and all other names
                var groups = grouping.GroupBy(x => ResolutionRegex().IsMatch(x.Files[0].FileNameWithoutExtension));
                foreach (var group in groups)
                {
                    if (group.Key)
                    {
                        alternateVersions.InsertRange(0, group
                            .OrderByDescending(x => ResolutionRegex().Match(x.Files[0].FileNameWithoutExtension.ToString()).Value, new AlphanumericComparator())
                            .ThenBy(x => x.Files[0].FileNameWithoutExtension.ToString(), new AlphanumericComparator()));
                    }
                    else
                    {
                        alternateVersions.AddRange(group.OrderBy(x => x.Files[0].FileNameWithoutExtension.ToString(), new AlphanumericComparator()));
                    }
                }
            }

            groupPrimary ??= alternateVersions.FirstOrDefault() ?? grouping.First();
            alternateVersions.Remove(groupPrimary);
            groupPrimary.AlternateVersions = alternateVersions.Select(x => x.Files[0]).ToArray();

            groupPrimary.Name = name.ToString();
            return groupPrimary;
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

        private static bool IsEligibleForMultiVersion(ReadOnlySpan<char> folderName, ReadOnlySpan<char> testFilePath, NamingOptions namingOptions, ReadOnlySpan<char> collectionType)
        {
            var testFilename = Path.GetFileNameWithoutExtension(testFilePath);

            if (collectionType.Equals(CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
            {
                // episodes are always eligible to be grouped
                return true;
            }

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
            if (CleanStringParser.TryClean(testFilename.ToString(), namingOptions.CleanStringRegexes, out var cleanName))
            {
                testFilename = cleanName.AsSpan().Trim();
            }

            // The CleanStringParser should have removed common keywords etc.
            return testFilename.IsEmpty
                   || testFilename[0] == '-'
                   || CheckMultiVersionRegex().IsMatch(testFilename);
        }

        private static string EpisodeGrouper(string testFilePath, NamingOptions namingOptions, ReadOnlySpan<char> collectionType)
        {
            // grouper for tv shows/episodes should be everything before space-dash-space
            var resolver = new EpisodeResolver(namingOptions);
            EpisodeInfo? episodeInfo = resolver.Resolve(testFilePath, false);
            ReadOnlySpan<char> seriesName = episodeInfo!.SeriesName;

            var filename = Path.GetFileNameWithoutExtension(testFilePath);
            // start with grouping by filename
            string g = filename;
            for (var i = 0; i < namingOptions.VideoVersionRegexes.Length; i++)
            {
                var rule = namingOptions.VideoVersionRegexes[i];
                var match = rule.Match(filename);
                if (!match.Success)
                {
                    continue;
                }

                g = match.Groups["filename"].Value;
                // clean the filename
                if (VideoResolver.TryCleanString(g, namingOptions, out string newName))
                {
                    g = newName;
                }

                // never group episodes under series name
                if (MemoryExtensions.Equals(g.AsSpan(), seriesName, StringComparison.OrdinalIgnoreCase))
                {
                    g = filename;
                }
            }

            return g;
        }
    }
}
