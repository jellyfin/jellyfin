using System;
using System.IO;
using Emby.Naming.Common;

namespace Emby.Naming.Video
{
    public class FlagParser
    {
        private readonly NamingOptions _options;

        public FlagParser(NamingOptions options)
        {
            _options = options;
        }

        public string[] GetFlags(string path)
        {
            return GetFlags(path, _options.VideoFlagDelimiters);
        }

        public string[] GetFlags(string path, char[] delimiters)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Array.Empty<string>();
            }

            // Note: the tags need be be surrounded be either a space ( ), hyphen -, dot . or underscore _.

            var file = Path.GetFileName(path);

            return file.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
