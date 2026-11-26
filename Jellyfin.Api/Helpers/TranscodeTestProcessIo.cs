using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Reads how much an ffmpeg process has read, where the system tells.
/// </summary>
internal static class TranscodeTestProcessIo
{
    /// <summary>
    /// Gets how many bytes a process has read, from rchar in /proc/&lt;pid&gt;/io. rchar counts every read, also from the
    /// page cache; read_bytes would leave out media that is already in memory.
    /// </summary>
    /// <param name="process">The process.</param>
    /// <returns>The bytes read; null on systems other than Linux, or when the file cannot be read, for example
    /// because the process has exited.</returns>
    public static long? BytesRead(Process process)
    {
        if (!OperatingSystem.IsLinux())
        {
            return null;
        }

        try
        {
            return ParseBytesRead(File.ReadAllText(string.Create(CultureInfo.InvariantCulture, $"/proc/{process.Id}/io")));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets rchar from the text of /proc/&lt;pid&gt;/io.
    /// </summary>
    /// <param name="text">The file's text, such as "rchar: 1024\nwchar: 0\n…".</param>
    /// <returns>The bytes read, or null when the text has no rchar line.</returns>
    internal static long? ParseBytesRead(string text)
    {
        foreach (var line in text.AsSpan().EnumerateLines())
        {
            if (line.StartsWith("rchar:", StringComparison.Ordinal)
                && long.TryParse(line["rchar:".Length..], NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out var bytes))
            {
                return bytes;
            }
        }

        return null;
    }

    /// <summary>
    /// Formats a byte count for the log, such as "0 B", "850 MB" or "1.02 GB".
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>The text, in decimal units.</returns>
    internal static string FormatBytes(long bytes) => bytes switch
    {
        < 1000 => string.Create(CultureInfo.InvariantCulture, $"{bytes} B"),
        < 999_500 => string.Create(CultureInfo.InvariantCulture, $"{bytes / 1e3:0} kB"),
        < 999_500_000 => string.Create(CultureInfo.InvariantCulture, $"{bytes / 1e6:0} MB"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{bytes / 1e9:0.00} GB")
    };
}
