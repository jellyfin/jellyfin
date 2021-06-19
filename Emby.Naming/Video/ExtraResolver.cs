using System;
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
    public class ExtraResolver
    {
        private readonly NamingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtraResolver"/> class.
        /// </summary>
        /// <param name="options"><see cref="NamingOptions"/> object containing VideoExtraRules and passed to <see cref="AudioFileParser"/> and <see cref="VideoResolver"/>.</param>
        public ExtraResolver(NamingOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Attempts to resolve if file is extra.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>Returns <see cref="ExtraResult"/> object.</returns>
        public ExtraResult GetExtraInfo(string path)
        {
            var result = new ExtraResult();

            for (var i = 0; i < _options.VideoExtraRules.Length; i++)
            {
                var rule = _options.VideoExtraRules[i];
                if (rule.MediaType == MediaType.Audio)
                {
                    if (!AudioFileParser.IsAudioFile(path, _options))
                    {
                        continue;
                    }
                }
                else if (rule.MediaType == MediaType.Video)
                {
                    if (!VideoResolver.IsVideoFile(path, _options))
                    {
                        continue;
                    }
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
                    var filename = Path.GetFileNameWithoutExtension(pathSpan);

                    if (filename.Contains(rule.Token, StringComparison.OrdinalIgnoreCase))
                    {
                        result.ExtraType = rule.ExtraType;
                        result.Rule = rule;
                    }
                }
                else if (rule.RuleType == ExtraRuleType.Regex)
                {
                    var filename = Path.GetFileName(path);

                    var regex = new Regex(rule.Token, RegexOptions.IgnoreCase);

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
    }
}
