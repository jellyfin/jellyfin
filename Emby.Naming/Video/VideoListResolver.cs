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
    /// <summary>
    /// Resolves alternative versions and extras from list of video files.
    /// </summary>
    public static class VideoListResolver
    {
        /// <summary>
        /// Resolves alternative versions and extras from list of video files.
        /// </summary>
        /// <param name="files">List of related video files.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="supportMultiVersion">Indication we should consider multi-versions of content.</param>
        /// <returns>Returns enumerable of <see cref="VideoInfo"/> which groups files together when related.</returns>
        public static IEnumerable<VideoInfo> Resolve(List<FileSystemMetadata> files, NamingOptions namingOptions, bool supportMultiVersion = true)
        {
            var videoInfos = files
                .Select(i => VideoResolver.Resolve(i.FullName, i.IsDirectory, namingOptions))
                .OfType<VideoFileInfo>()
                .ToList();

            // Filter out all extras, otherwise they could cause stacks to not be resolved
            // See the unit test TestStackedWithTrailer
            var nonExtras = videoInfos
                .Where(i => i.ExtraType == null)
                .Select(i => new FileSystemMetadata { FullName = i.Path, IsDirectory = i.IsDirectory });

            var stackResult = new StackResolver(namingOptions)
                .Resolve(nonExtras).ToList();

            var remainingFiles = videoInfos
                .Where(i => !stackResult.Any(s => i.Path != null && s.ContainsFile(i.Path, i.IsDirectory)))
                .ToList();

            var list = new List<VideoInfo>();

            foreach (var stack in stackResult)
            {
                var info = new VideoInfo(stack.Name)
                {
                    Files = stack.Files.Select(i => VideoResolver.Resolve(i, stack.IsDirectoryStack, namingOptions))
                        .OfType<VideoFileInfo>()
                        .ToList()
                };

                info.Year = info.Files[0].Year;

                var extras = ExtractExtras(remainingFiles, stack.Name, Path.GetFileNameWithoutExtension(stack.Files[0].AsSpan()), namingOptions.VideoFlagDelimiters);

                if (extras.Count > 0)
                {
                    info.Extras = extras;
                }

                list.Add(info);
            }

            var standaloneMedia = remainingFiles
                .Where(i => i.ExtraType == null)
                .ToList();

            foreach (var media in standaloneMedia)
            {
                var info = new VideoInfo(media.Name) { Files = new[] { media } };

                info.Year = info.Files[0].Year;

                remainingFiles.Remove(media);
                var extras = ExtractExtras(remainingFiles, media.FileNameWithoutExtension, namingOptions.VideoFlagDelimiters);

                info.Extras = extras;

                list.Add(info);
            }

            if (supportMultiVersion)
            {
                list = GetVideosGroupedByVersion(list, namingOptions);
            }

            // If there's only one resolved video, use the folder name as well to find extras
            if (list.Count == 1)
            {
                var info = list[0];
                var videoPath = list[0].Files[0].Path;
                var parentPath = Path.GetDirectoryName(videoPath.AsSpan());

                if (!parentPath.IsEmpty)
                {
                    var folderName = Path.GetFileName(parentPath);
                    if (!folderName.IsEmpty)
                    {
                        var extras = ExtractExtras(remainingFiles, folderName, namingOptions.VideoFlagDelimiters);
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
            // Be lenient because people use all kinds of mishmash conventions with trailers.
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
                Files = new[] { i },
                Year = i.Year
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
            for (var i = 0; i < videos.Count; i++)
            {
                var video = videos[i];
                if (!IsEligibleForMultiVersion(folderName, video.Files[0].Path, namingOptions))
                {
                    return videos;
                }
            }

            // The list is created and overwritten in the caller, so we are allowed to do in-place sorting
            videos.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

            var list = new List<VideoInfo>
            {
                videos[0]
            };

            var alternateVersionsLen = videos.Count - 1;
            var alternateVersions = new VideoFileInfo[alternateVersionsLen];
            var extras = new List<VideoFileInfo>(list[0].Extras);
            for (int i = 0; i < alternateVersionsLen; i++)
            {
                var video = videos[i + 1];
                alternateVersions[i] = video.Files[0];
                extras.AddRange(video.Extras);
            }

            list[0].AlternateVersions = alternateVersions;
            list[0].Name = folderName.ToString();
            list[0].Extras = extras;

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

        private static bool IsEligibleForMultiVersion(ReadOnlySpan<char> folderName, string testFilePath, NamingOptions namingOptions)
        {
            var testFilename = Path.GetFileNameWithoutExtension(testFilePath.AsSpan());
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
            var tmpTestFilename = testFilename.ToString();
            if (CleanStringParser.TryClean(tmpTestFilename, namingOptions.CleanStringRegexes, out var cleanName))
            {
                tmpTestFilename = cleanName.Trim().ToString();
            }

            // The CleanStringParser should have removed common keywords etc.
            return string.IsNullOrEmpty(tmpTestFilename)
                   || testFilename[0] == '-'
                   || Regex.IsMatch(tmpTestFilename, @"^\[([^]]*)\]", RegexOptions.Compiled);
        }

        private static ReadOnlySpan<char> TrimFilenameDelimiters(ReadOnlySpan<char> name, ReadOnlySpan<char> videoFlagDelimiters)
        {
            return name.IsEmpty ? name : name.TrimEnd().TrimEnd(videoFlagDelimiters).TrimEnd();
        }

        private static bool StartsWith(ReadOnlySpan<char> fileName, ReadOnlySpan<char> baseName, ReadOnlySpan<char> trimmedBaseName)
        {
            if (baseName.IsEmpty)
            {
                return false;
            }

            return fileName.StartsWith(baseName, StringComparison.OrdinalIgnoreCase)
                   || (!trimmedBaseName.IsEmpty && fileName.StartsWith(trimmedBaseName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Finds similar filenames to that of [baseName] and removes any matches from [remainingFiles].
        /// </summary>
        /// <param name="remainingFiles">The list of remaining filenames.</param>
        /// <param name="baseName">The base name to use for the comparison.</param>
        /// <param name="videoFlagDelimiters">The video flag delimiters.</param>
        /// <returns>A list of video extras for [baseName].</returns>
        private static List<VideoFileInfo> ExtractExtras(IList<VideoFileInfo> remainingFiles, ReadOnlySpan<char> baseName, ReadOnlySpan<char> videoFlagDelimiters)
        {
            return ExtractExtras(remainingFiles, baseName, ReadOnlySpan<char>.Empty, videoFlagDelimiters);
        }

        /// <summary>
        /// Finds similar filenames to that of [firstBaseName] and [secondBaseName] and removes any matches from [remainingFiles].
        /// </summary>
        /// <param name="remainingFiles">The list of remaining filenames.</param>
        /// <param name="firstBaseName">The first base name to use for the comparison.</param>
        /// <param name="secondBaseName">The second base name to use for the comparison.</param>
        /// <param name="videoFlagDelimiters">The video flag delimiters.</param>
        /// <returns>A list of video extras for [firstBaseName] and [secondBaseName].</returns>
        private static List<VideoFileInfo> ExtractExtras(IList<VideoFileInfo> remainingFiles, ReadOnlySpan<char> firstBaseName, ReadOnlySpan<char> secondBaseName, ReadOnlySpan<char> videoFlagDelimiters)
        {
            var trimmedFirstBaseName = TrimFilenameDelimiters(firstBaseName, videoFlagDelimiters);
            var trimmedSecondBaseName = TrimFilenameDelimiters(secondBaseName, videoFlagDelimiters);

            var result = new List<VideoFileInfo>();
            for (var pos = remainingFiles.Count - 1; pos >= 0; pos--)
            {
                var file = remainingFiles[pos];
                if (file.ExtraType == null)
                {
                    continue;
                }

                var filename = file.FileNameWithoutExtension;
                if (StartsWith(filename, firstBaseName, trimmedFirstBaseName)
                    || StartsWith(filename, secondBaseName, trimmedSecondBaseName))
                {
                    result.Add(file);
                    remainingFiles.RemoveAt(pos);
                }
            }

            return result;
        }
    }
}
