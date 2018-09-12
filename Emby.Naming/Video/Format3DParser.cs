using Emby.Naming.Common;
using System;
using System.Linq;

namespace Emby.Naming.Video
{
    public class Format3DParser
    {
        private readonly NamingOptions _options;

        public Format3DParser(NamingOptions options)
        {
            _options = options;
        }

        public Format3DResult Parse(string path)
        {
            var delimeters = _options.VideoFlagDelimiters.ToList();
            delimeters.Add(' ');

            return Parse(new FlagParser(_options).GetFlags(path, delimeters.ToArray()));
        }

        internal Format3DResult Parse(string[] videoFlags)
        {
            foreach (var rule in _options.Format3DRules)
            {
                var result = Parse(videoFlags, rule);

                if (result.Is3D)
                {
                    return result;
                }
            }

            return new Format3DResult();
        }

        private Format3DResult Parse(string[] videoFlags, Format3DRule rule)
        {
            var result = new Format3DResult();

            if (string.IsNullOrEmpty(rule.PreceedingToken))
            {
                result.Format3D = new[] { rule.Token }.FirstOrDefault(i => videoFlags.Contains(i, StringComparer.OrdinalIgnoreCase));
                result.Is3D = !string.IsNullOrEmpty(result.Format3D);

                if (result.Is3D)
                {
                    result.Tokens.Add(rule.Token);
                }
            }
            else
            {
                var foundPrefix = false;
                string format = null;

                foreach (var flag in videoFlags)
                {
                    if (foundPrefix)
                    {
                        result.Tokens.Add(rule.PreceedingToken);

                        if (string.Equals(rule.Token, flag, StringComparison.OrdinalIgnoreCase))
                        {
                            format = flag;
                            result.Tokens.Add(rule.Token);
                        }
                        break;
                    }
                    foundPrefix = string.Equals(flag, rule.PreceedingToken, StringComparison.OrdinalIgnoreCase);
                }

                result.Is3D = foundPrefix && !string.IsNullOrEmpty(format);
                result.Format3D = format;
            }

            return result;
        }
    }
}
