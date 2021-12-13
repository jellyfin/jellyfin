using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.Naming.Audio;
using Emby.Naming.Common;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Resolve if file is extra for video.
    /// </summary>
    public static class ExtraResolver
    {
        private static readonly char[] _digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        /// <summary>
        /// Attempts to resolve if file is extra.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <returns>Returns <see cref="ExtraResult"/> object.</returns>
        public static ExtraResult GetExtraInfo(string path, NamingOptions namingOptions)
        {
            var result = new ExtraResult();

            for (var i = 0; i < namingOptions.VideoExtraRules.Length; i++)
            {
                var rule = namingOptions.VideoExtraRules[i];
                if ((rule.MediaType == MediaType.Audio && !AudioFileParser.IsAudioFile(path, namingOptions))
                    || (rule.MediaType == MediaType.Video && !VideoResolver.IsVideoFile(path, namingOptions)))
                {
                    continue;
                }

                var pathSpan = path.AsSpan();
                if (rule.RuleType == ExtraRuleType.Filename)
                {
                    var filename = Path.GetFileNameWithoutExtension(pathSpan);

                    if (filename.Equals(rule.Token, StringComparison.OrdinalIgnoreCase))
                    {
                        result.ExtraType = rule.ExtraType;
                        result.Rule = rule;
                    }
                }
                else if (rule.RuleType == ExtraRuleType.Suffix)
                {
                    // Trim the digits from the end of the filename so we can recognize things like -trailer2
                    var filename = Path.GetFileNameWithoutExtension(pathSpan).TrimEnd(_digits);

                    if (filename.EndsWith(rule.Token, StringComparison.OrdinalIgnoreCase))
                    {
                        result.ExtraType = rule.ExtraType;
                        result.Rule = rule;
                    }
                }
                else if (rule.RuleType == ExtraRuleType.Regex)
                {
                    var filename = Path.GetFileName(path);

                    var regex = new Regex(rule.Token, RegexOptions.IgnoreCase | RegexOptions.Compiled);

                    if (regex.IsMatch(filename))
                    {
                        result.ExtraType = rule.ExtraType;
                        result.Rule = rule;
                    }
                }
                else if (rule.RuleType == ExtraRuleType.DirectoryName)
                {
                    var directoryName = Path.GetFileName(Path.GetDirectoryName(pathSpan));
                    if (directoryName.Equals(rule.Token, StringComparison.OrdinalIgnoreCase))
                    {
                        result.ExtraType = rule.ExtraType;
                        result.Rule = rule;
                    }
                }

                if (result.ExtraType != null)
                {
                    return result;
                }
            }

            return result;
        }

        /// <summary>
        /// Finds extras matching the video info.
        /// </summary>
        /// <param name="files">The list of file video infos.</param>
        /// <param name="videoInfo">The video to compare against.</param>
        /// <param name="videoFlagDelimiters">The video flag delimiters.</param>
        /// <returns>A list of video extras for [videoInfo].</returns>
        public static IReadOnlyList<VideoFileInfo> GetExtras(IReadOnlyList<VideoInfo> files, VideoFileInfo videoInfo, ReadOnlySpan<char> videoFlagDelimiters)
        {
            var parentDir = videoInfo.IsDirectory ? videoInfo.Path : Path.GetDirectoryName(videoInfo.Path.AsSpan());

            var trimmedFileNameWithoutExtension = TrimFilenameDelimiters(videoInfo.FileNameWithoutExtension, videoFlagDelimiters);
            var trimmedVideoInfoName = TrimFilenameDelimiters(videoInfo.Name, videoFlagDelimiters);

            var result = new List<VideoFileInfo>();
            for (var pos = files.Count - 1; pos >= 0; pos--)
            {
                var current = files[pos];
                // ignore non-extras and multi-file (can this happen?)
                if (current.ExtraType == null || current.Files.Count > 1)
                {
                    continue;
                }

                var currentFile = current.Files[0];
                var trimmedCurrentFileName = TrimFilenameDelimiters(currentFile.Name, videoFlagDelimiters);

                // first check filenames
                bool isValid = StartsWith(trimmedCurrentFileName, trimmedFileNameWithoutExtension)
                               || (StartsWith(trimmedCurrentFileName, trimmedVideoInfoName) && currentFile.Year == videoInfo.Year);

                // then by directory
                if (!isValid)
                {
                    // When the extra rule type is DirectoryName we must go one level higher to get the "real" dir name
                    var currentParentDir = currentFile.ExtraRule?.RuleType == ExtraRuleType.DirectoryName
                        ? Path.GetDirectoryName(Path.GetDirectoryName(currentFile.Path.AsSpan()))
                        : Path.GetDirectoryName(currentFile.Path.AsSpan());

                    isValid = !currentParentDir.IsEmpty && !parentDir.IsEmpty && currentParentDir.Equals(parentDir, StringComparison.OrdinalIgnoreCase);
                }

                if (isValid)
                {
                    result.Add(currentFile);
                }
            }

            return result.OrderBy(r => r.Path).ToArray();
        }

        private static ReadOnlySpan<char> TrimFilenameDelimiters(ReadOnlySpan<char> name, ReadOnlySpan<char> videoFlagDelimiters)
        {
            return name.IsEmpty ? name : name.TrimEnd().TrimEnd(videoFlagDelimiters).TrimEnd();
        }

        private static bool StartsWith(ReadOnlySpan<char> fileName, ReadOnlySpan<char> baseName)
        {
            return !baseName.IsEmpty && fileName.StartsWith(baseName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
