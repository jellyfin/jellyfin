#nullable enable
#pragma warning disable CS1591

using System;
using System.IO;
using System.Linq;
using Emby.Naming.Common;

namespace Emby.Naming.Audio
{
    public static class AudioFileParser
    {
        public static bool IsAudioFile(string path, NamingOptions options)
        {
            var extension = Path.GetExtension(path);
            return options.AudioFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }
    }
}
