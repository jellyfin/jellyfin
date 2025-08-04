using System;
using System.IO;
using System.Text.RegularExpressions;
using Emby.Naming.Audio;
using Emby.Naming.Common;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Resolve if file is extra for video.
    /// </summary>
    public static class ExtraRuleResolver
    {
        private static readonly char[] _digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        /// <summary>
        /// Attempts to resolve if file is extra.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="libraryRoot">Top-level folder for the containing library.</param>
        /// <returns>Returns <see cref="ExtraResult"/> object.</returns>
        public static ExtraResult GetExtraInfo(string path, NamingOptions namingOptions, string? libraryRoot = "")
        {
            ExtraResult result = new ExtraResult();

            bool isAudioFile = AudioFileParser.IsAudioFile(path, namingOptions);
            bool isVideoFile = VideoResolver.IsVideoFile(path, namingOptions);

            ReadOnlySpan<char> pathSpan = path.AsSpan();
            ReadOnlySpan<char> fileName = Path.GetFileName(pathSpan);
            ReadOnlySpan<char> fileNameWithoutExtension = Path.GetFileNameWithoutExtension(pathSpan);
            // Trim the digits from the end of the filename so we can recognize things like -trailer2
            ReadOnlySpan<char> trimmedFileNameWithoutExtension = fileNameWithoutExtension.TrimEnd(_digits);
            ReadOnlySpan<char> directoryName = Path.GetFileName(Path.GetDirectoryName(pathSpan));
            string fullDirectory = Path.GetDirectoryName(pathSpan).ToString();

            foreach (ExtraRule rule in namingOptions.VideoExtraRules)
            {
                if ((rule.MediaType == MediaType.Audio && !isAudioFile)
                    || (rule.MediaType == MediaType.Video && !isVideoFile))
                {
                    continue;
                }

                bool isMatch = rule.RuleType switch
                {
                    ExtraRuleType.Filename => fileNameWithoutExtension.Equals(rule.Token, StringComparison.OrdinalIgnoreCase),
                    ExtraRuleType.Suffix => trimmedFileNameWithoutExtension.EndsWith(rule.Token, StringComparison.OrdinalIgnoreCase),
                    ExtraRuleType.Regex => Regex.IsMatch(fileName, rule.Token, RegexOptions.IgnoreCase | RegexOptions.Compiled),
                    ExtraRuleType.DirectoryName => directoryName.Equals(rule.Token, StringComparison.OrdinalIgnoreCase)
                                                 && !string.Equals(fullDirectory, libraryRoot, StringComparison.OrdinalIgnoreCase),
                    _ => false,
                };

                if (!isMatch)
                {
                    continue;
                }

                result.ExtraType = rule.ExtraType;
                result.Rule = rule;
                return result;
            }

            return result;
        }
    }
}
