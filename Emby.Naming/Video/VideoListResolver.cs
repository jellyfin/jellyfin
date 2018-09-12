using Emby.Naming.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Model.IO;
using System.Text.RegularExpressions;

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
                .Where(i => string.IsNullOrEmpty(i.ExtraType))
                .Select(i => new FileSystemMetadata
                {
                    FullName = i.Path,
                    IsDirectory = i.IsDirectory
                });

            var stackResult = new StackResolver(_options)
                .Resolve(nonExtras);

            var remainingFiles = videoInfos
                .Where(i => !stackResult.Stacks.Any(s => s.ContainsFile(i.Path, i.IsDirectory)))
                .ToList();

            var list = new List<VideoInfo>();

            foreach (var stack in stackResult.Stacks)
            {
                var info = new VideoInfo
                {
                    Files = stack.Files.Select(i => videoResolver.Resolve(i, stack.IsDirectoryStack)).ToList(),
                    Name = stack.Name
                };

                info.Year = info.Files.First().Year;

                var extraBaseNames = new List<string> 
                {
                    stack.Name, 
                    Path.GetFileNameWithoutExtension(stack.Files[0])
                };

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
                .Where(i => string.IsNullOrEmpty(i.ExtraType))
                .ToList();

            foreach (var media in standaloneMedia)
            {
                var info = new VideoInfo
                {
                    Files = new List<VideoFileInfo> { media },
                    Name = media.Name
                };

                info.Year = info.Files.First().Year;

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
                    var folderName = Path.GetFileName(Path.GetDirectoryName(videoPath));
                    if (!string.IsNullOrEmpty(folderName))
                    {
                        var extras = GetExtras(remainingFiles, new List<string> { folderName });

                        remainingFiles = remainingFiles
                            .Except(extras)
                            .ToList();

                        info.Extras.AddRange(extras);
                    }
                }

                // Add the extras that are just based on file name as well
                var extrasByFileName = remainingFiles
                    .Where(i => i.ExtraRule != null && i.ExtraRule.RuleType == ExtraRuleType.Filename)
                    .ToList();

                remainingFiles = remainingFiles
                    .Except(extrasByFileName)
                    .ToList();

                info.Extras.AddRange(extrasByFileName);
            }

            // If there's only one video, accept all trailers
            // Be lenient because people use all kinds of mish mash conventions with trailers
            if (list.Count == 1)
            {
                var trailers = remainingFiles
                    .Where(i => string.Equals(i.ExtraType, "trailer", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                list[0].Extras.AddRange(trailers);

                remainingFiles = remainingFiles
                    .Except(trailers)
                    .ToList();
            }

            // Whatever files are left, just add them
            list.AddRange(remainingFiles.Select(i => new VideoInfo
            {
                Files = new List<VideoFileInfo> { i },
                Name = i.Name,
                Year = i.Year
            }));

            var orderedList = list.OrderBy(i => i.Name);

            return orderedList;
        }

        private IEnumerable<VideoInfo> GetVideosGroupedByVersion(List<VideoInfo> videos)
        {
            if (videos.Count == 0)
            {
                return videos;
            }

            var list = new List<VideoInfo>();

            var folderName = Path.GetFileName(Path.GetDirectoryName(videos[0].Files[0].Path));

            if (!string.IsNullOrEmpty(folderName) && folderName.Length > 1)
            {
                if (videos.All(i => i.Files.Count == 1 && IsEligibleForMultiVersion(folderName, i.Files[0].Path)))
                {
                    // Enforce the multi-version limit
                    if (videos.Count <= 8 && HaveSameYear(videos))
                    {
                        var ordered = videos.OrderBy(i => i.Name).ToList();

                        list.Add(ordered[0]);

                        list[0].AlternateVersions = ordered.Skip(1).Select(i => i.Files[0]).ToList();
                        list[0].Name = folderName;
                        list[0].Extras.AddRange(ordered.Skip(1).SelectMany(i => i.Extras));

                        return list;
                    }
                }
            }

            return videos;
            //foreach (var video in videos.OrderBy(i => i.Name))
            //{
            //    var match = list
            //        .FirstOrDefault(i => string.Equals(i.Name, video.Name, StringComparison.OrdinalIgnoreCase));

            //    if (match != null && video.Files.Count == 1 && match.Files.Count == 1)
            //    {
            //        match.AlternateVersions.Add(video.Files[0]);
            //        match.Extras.AddRange(video.Extras);
            //    }
            //    else
            //    {
            //        list.Add(video);
            //    }
            //}

            //return list;
        }

        private bool HaveSameYear(List<VideoInfo> videos)
        {
            return videos.Select(i => i.Year ?? -1).Distinct().Count() < 2;
        }

        private bool IsEligibleForMultiVersion(string folderName, string testFilename)
        {
            testFilename = Path.GetFileNameWithoutExtension(testFilename);

            if (string.Equals(folderName, testFilename, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (testFilename.StartsWith(folderName, StringComparison.OrdinalIgnoreCase))
            {
                testFilename = testFilename.Substring(folderName.Length).Trim();
                return testFilename.StartsWith("-", StringComparison.OrdinalIgnoreCase)||Regex.Replace(testFilename, @"\[([^]]*)\]", "").Trim() == String.Empty;
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
                .Where(i => !string.IsNullOrEmpty(i.ExtraType))
                .Where(i => baseNames.Any(b => i.FileNameWithoutExtension.StartsWith(b, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }
}
