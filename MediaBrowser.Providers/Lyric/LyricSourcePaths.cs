using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Providers.Lyric;

internal static class LyricSourcePaths
{
    private static readonly string[] AudioExtensions = [".mp3", ".m4a", ".flac", ".ogg", ".opus", ".wav", ".aac", ".wma", ".aiff", ".alac", ".ape"];

    internal static IEnumerable<string> Find(string audioPath, IReadOnlyList<FileSystemMetadata> files, IReadOnlyCollection<string> lyricExtensions)
    {
        var stem = Path.GetFileNameWithoutExtension(audioPath);
        var key = LyricMerger.FileKey(stem);
        var unambiguous = files.Count(f => AudioExtensions.Contains(Path.GetExtension(f.FullName), StringComparer.OrdinalIgnoreCase)
            && LyricMerger.FileKey(Path.GetFileNameWithoutExtension(f.FullName)) == key) == 1;
        foreach (var file in files)
        {
            if (!lyricExtensions.Contains(Path.GetExtension(file.FullName), StringComparer.OrdinalIgnoreCase)) continue;
            var lyricStem = Path.GetFileNameWithoutExtension(file.FullName);
            if (lyricStem.Equals(stem, StringComparison.OrdinalIgnoreCase)
                || (unambiguous && key.Length > 0 && LyricMerger.FileKey(lyricStem) == key))
            {
                yield return file.FullName;
            }
        }
    }
}
