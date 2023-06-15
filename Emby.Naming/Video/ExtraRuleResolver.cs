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
                    var filename = Path.GetFileName(path.AsSpan());

                    var isMatch = Regex.IsMatch(filename, rule.Token, RegexOptions.IgnoreCase | RegexOptions.Compiled);

                    if (isMatch)
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

                if (result.ExtraType is not null)
                {
                    return result;
                }
            }

            return result;
        }
    }
}
