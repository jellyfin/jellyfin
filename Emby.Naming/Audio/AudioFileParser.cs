#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.IO;
using System.Linq;
using Emby.Naming.Common;

namespace Emby.Naming.Audio
{
    public class AudioFileParser
    {
        private readonly NamingOptions _options;

        public AudioFileParser(NamingOptions options)
        {
            _options = options;
        }

        public bool IsAudioFile(string path)
        {
            var extension = Path.GetExtension(path) ?? string.Empty;
            return _options.AudioFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }
    }
}
