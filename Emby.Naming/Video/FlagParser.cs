using Emby.Naming.Common;
using System;
using System.IO;

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

        public string[] GetFlags(string path, char[] delimeters)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            // Note: the tags need be be surrounded be either a space ( ), hyphen -, dot . or underscore _.

            var file = Path.GetFileName(path);

            return file.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
