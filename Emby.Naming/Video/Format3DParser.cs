using System;
using System.Linq;
using Emby.Naming.Common;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Parste 3D format related flags.
    /// </summary>
    public class Format3DParser
    {
        private readonly NamingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="Format3DParser"/> class.
        /// </summary>
        /// <param name="options"><see cref="NamingOptions"/> object containing VideoFlagDelimiters and passes options to <see cref="FlagParser"/>.</param>
        public Format3DParser(NamingOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Parse 3D format related flags.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>Returns <see cref="Format3DResult"/> object.</returns>
        public Format3DResult Parse(string path)
        {
            int oldLen = _options.VideoFlagDelimiters.Length;
            var delimiters = new char[oldLen + 1];
            _options.VideoFlagDelimiters.CopyTo(delimiters, 0);
            delimiters[oldLen] = ' ';

            return Parse(new FlagParser(_options).GetFlags(path, delimiters));
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

        private static Format3DResult Parse(string[] videoFlags, Format3DRule rule)
        {
            var result = new Format3DResult();

            if (string.IsNullOrEmpty(rule.PrecedingToken))
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
                string? format = null;

                foreach (var flag in videoFlags)
                {
                    if (foundPrefix)
                    {
                        result.Tokens.Add(rule.PrecedingToken);

                        if (string.Equals(rule.Token, flag, StringComparison.OrdinalIgnoreCase))
                        {
                            format = flag;
                            result.Tokens.Add(rule.Token);
                        }

                        break;
                    }

                    foundPrefix = string.Equals(flag, rule.PrecedingToken, StringComparison.OrdinalIgnoreCase);
                }

                result.Is3D = foundPrefix && !string.IsNullOrEmpty(format);
                result.Format3D = format;
            }

            return result;
        }
    }
}
