using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Books;

internal static class AudioBookCueChapterParser
{
    private const long TicksPerSecond = 10_000_000L;
    private const long TicksPerFrame = TicksPerSecond / 75;

    internal static IReadOnlyList<ChapterInfo> ParseCueSidecar(string audioPath)
    {
        var cuePath = FindCueSidecar(audioPath);
        if (cuePath is null)
        {
            return Array.Empty<ChapterInfo>();
        }

        return ParseCueFile(cuePath);
    }

    private static string? FindCueSidecar(string audioPath)
    {
        var dir = Path.GetDirectoryName(audioPath);
        if (dir is null)
        {
            return null;
        }

        var sameName = Path.ChangeExtension(audioPath, ".cue");
        if (File.Exists(sameName))
        {
            return sameName;
        }

        var cueFiles = Directory.GetFiles(dir, "*.cue");
        return cueFiles.Length == 1 ? cueFiles[0] : null;
    }

    private static IReadOnlyList<ChapterInfo> ParseCueFile(string cuePath)
    {
        var chapters = new List<ChapterInfo>();
        ChapterInfo? current = null;

        foreach (var line in File.ReadLines(cuePath))
        {
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("TRACK ", StringComparison.OrdinalIgnoreCase))
            {
                current = new ChapterInfo();
                chapters.Add(current);
            }
            else if (current is not null && trimmed.StartsWith("TITLE ", StringComparison.OrdinalIgnoreCase))
            {
                current.Name = trimmed[6..].Trim('"', ' ');
            }
            else if (current is not null && trimmed.StartsWith("INDEX 01 ", StringComparison.OrdinalIgnoreCase))
            {
                current.StartPositionTicks = ParseTimestamp(trimmed[9..]);
            }
        }

        return chapters;
    }

    private static long ParseTimestamp(string timestamp)
    {
        var parts = timestamp.Trim().Split(':');
        if (parts.Length != 3
            || !int.TryParse(parts[0], out var minutes)
            || !int.TryParse(parts[1], out var seconds)
            || !int.TryParse(parts[2], out var frames))
        {
            return 0;
        }

        return (((minutes * 60L) + seconds) * TicksPerSecond) + (frames * TicksPerFrame);
    }
}
