#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.Naming.Common;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace Emby.Naming.Video
{
    public class VideoListResolver
    {
        private readonly NamingOptions _options;

        public VideoListResolver(NamingOptions options)
        {
            _options = options;
        }

        public IEnumerable<VideoInfo> Resolve(List<FileSystemMetadata> files, bool supportMultiVersion = true)
        {
            var videoResolver = new VideoResolver(_options);

            var videoInfos = files
                .Select(i => videoResolver.Resolve(i.FullName, i.IsDirectory))
                .Where(i => i != null)
                .ToList();

            // Filter out all extras, otherwise they could cause stacks to not be resolved
            // See the unit test TestStackedWithTrailer
            var nonExtras = videoInfos
                .Where(i => i.ExtraType == null)
                .Select(i => new FileSystemMetadata { FullName = i.Path, IsDirectory = i.IsDirectory });

            var stackResult = new StackResolver(_options)
                .Resolve(nonExtras).ToList();

            var remainingFiles = videoInfos
                .Where(i => !stackResult.Any(s => s.ContainsFile(i.Path, i.IsDirectory)))
                .ToList();

            var list = new List<VideoInfo>();

            foreach (var stack in stackResult)
            {
                var info = new VideoInfo(stack.Name)
                {
                    Files = stack.Files.Select(i => videoResolver.Resolve(i, stack.IsDirectoryStack)).ToList()
                };

                info.Year = info.Files[0].Year;

                var extraBaseNames = new List<string> { stack.Name, Path.GetFileNameWithoutExtension(stack.Files[0]) };

                var extras = GetExtras(remainingFiles, extraBaseNames);

                if (extras.Count > 0)
                {
                    remainingFiles = remainingFiles
                        .Except(extras)
                        .ToList();

                    info.Extras = extras;
                }

                list.Add(info);
            }

            var standaloneMedia = remainingFiles
                .Where(i => i.ExtraType == null)
                .ToList();

            foreach (var media in standaloneMedia)
            {
                var info = new VideoInfo(media.Name) { Files = new List<VideoFileInfo> { media } };

                info.Year = info.Files[0].Year;

                var extras = GetExtras(remainingFiles, new List<string> { media.FileNameWithoutExtension });

                remainingFiles = remainingFiles
                    .Except(extras.Concat(new[] { media }))
                    .ToList();

                info.Extras = extras;

                list.Add(info);
            }

            if (supportMultiVersion)
            {
                list = GetVideosGroupedByVersion(list)
                    .ToList();
            }

            // If there's only one resolved video, use the folder name as well to find extras
            if (list.Count == 1)
            {
                var info = list[0];
                var videoPath = list[0].Files[0].Path;
                var parentPath = Path.GetDirectoryName(videoPath);

                if (!string.IsNullOrEmpty(parentPath))
                {
                    var folderName = Path.GetFileName(parentPath);
                    if (!string.IsNullOrEmpty(folderName))
                    {
                        var extras = GetExtras(remainingFiles, new List<string> { folderName });

                        remainingFiles = remainingFiles
                            .Except(extras)
                            .ToList();

                        extras.AddRange(info.Extras);
                        info.Extras = extras;
                    }
                }

                // Add the extras that are just based on file name as well
                var extrasByFileName = remainingFiles
                    .Where(i => i.ExtraRule != null && i.ExtraRule.RuleType == ExtraRuleType.Filename)
                    .ToList();

                remainingFiles = remainingFiles
                    .Except(extrasByFileName)
                    .ToList();

                extrasByFileName.AddRange(info.Extras);
                info.Extras = extrasByFileName;
            }

            // If there's only one video, accept all trailers
            // Be lenient because people use all kinds of mish mash conventions with trailers
            if (list.Count == 1)
            {
                var trailers = remainingFiles
                    .Where(i => i.ExtraType == ExtraType.Trailer)
                    .ToList();

                trailers.AddRange(list[0].Extras);
                list[0].Extras = trailers;

                remainingFiles = remainingFiles
                    .Except(trailers)
                    .ToList();
            }

            // Whatever files are left, just add them
            list.AddRange(remainingFiles.Select(i => new VideoInfo(i.Name)
            {
                Files = new List<VideoFileInfo> { i },
                Year = i.Year
            }));

            return list;
        }

        private IEnumerable<VideoInfo> GetVideosGroupedByVersion(List<VideoInfo> videos)
        {
            if (videos.Count == 0)
            {
                return videos;
            }

            var list = new List<VideoInfo>();

            var folderName = Path.GetFileName(Path.GetDirectoryName(videos[0].Files[0].Path));

            if (!string.IsNullOrEmpty(folderName)
                && folderName.Length > 1
                && videos.All(i => i.Files.Count == 1
                && IsEligibleForMultiVersion(folderName, i.Files[0].Path))
                && HaveSameYear(videos))
            {
                var ordered = videos.OrderBy(i => i.Name).ToList();

                list.Add(ordered[0]);

                var alternateVersionsLen = ordered.Count - 1;
                var alternateVersions = new VideoFileInfo[alternateVersionsLen];
                for (int i = 0; i < alternateVersionsLen; i++)
                {
                    alternateVersions[i] = ordered[i + 1].Files[0];
                }

                list[0].AlternateVersions = alternateVersions;
                list[0].Name = folderName;
                var extras = ordered.Skip(1).SelectMany(i => i.Extras).ToList();
                extras.AddRange(list[0].Extras);
                list[0].Extras = extras;

                return list;
            }

            return videos;
        }

        private bool HaveSameYear(List<VideoInfo> videos)
        {
            return videos.Select(i => i.Year ?? -1).Distinct().Count() < 2;
        }

        private bool IsEligibleForMultiVersion(string folderName, string testFilename)
        {
            testFilename = Path.GetFileNameWithoutExtension(testFilename) ?? string.Empty;

            if (testFilename.StartsWith(folderName, StringComparison.OrdinalIgnoreCase))
            {
                testFilename = testFilename.Substring(folderName.Length).Trim();
                return string.IsNullOrEmpty(testFilename)
                   || testFilename[0] == '-'
                   || string.IsNullOrWhiteSpace(Regex.Replace(testFilename, @"\[([^]]*)\]", string.Empty));
            }

            return false;
        }

        private List<VideoFileInfo> GetExtras(IEnumerable<VideoFileInfo> remainingFiles, List<string> baseNames)
        {
            foreach (var name in baseNames.ToList())
            {
                var trimmedName = name.TrimEnd().TrimEnd(_options.VideoFlagDelimiters).TrimEnd();
                baseNames.Add(trimmedName);
            }

            return remainingFiles
                .Where(i => i.ExtraType != null)
                .Where(i => baseNames.Any(b =>
                    i.FileNameWithoutExtension.StartsWith(b, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }
}
