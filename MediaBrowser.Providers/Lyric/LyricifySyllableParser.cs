using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Lyrics;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// Parser for the Lyricify Syllable format.
/// </summary>
public partial class LyricifySyllableParser : ILyricParser
{
    private static readonly string[] _supportedMediaTypes = [".lrc"];
    private static readonly HashSet<string> _metadataTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "ar", "ti", "al", "offset", "length"
    };

    /// <inheritdoc />
    public string Name => "LyricifySyllableProvider";

    /// <inheritdoc />
    public ResolverPriority Priority => ResolverPriority.Third;

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedExtensions => _supportedMediaTypes;

    /// <inheritdoc />
    public LyricDto? ParseLyrics(LyricFile lyrics)
    {
        if (!_supportedMediaTypes.Contains(Path.GetExtension(lyrics.Name.AsSpan()), StringComparison.OrdinalIgnoreCase)
            || !DetectionRegex().IsMatch(lyrics.Content))
        {
            return null;
        }

        try
        {
            var mainLines = new List<LyricLine>();
            var backgroundLines = new List<LyricLine>();
            foreach (var rawLine in lyrics.Content.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || IsMetadata(line))
                {
                    continue;
                }

                var parsed = ParseLine(line);
                if (parsed is null)
                {
                    continue;
                }

                if (parsed.Value.IsBackground)
                {
                    backgroundLines.Add(parsed.Value.Line);
                }
                else
                {
                    mainLines.Add(parsed.Value.Line);
                }
            }

            if (mainLines.Count == 0 && backgroundLines.Count == 0)
            {
                return null;
            }

            var tracks = new List<LyricTrack>();
            if (mainLines.Count > 0)
            {
                tracks.Add(new LyricTrack { Type = LyricTrackType.Main, Lines = mainLines });
            }

            if (backgroundLines.Count > 0)
            {
                tracks.Add(new LyricTrack { Type = LyricTrackType.Background, Lines = backgroundLines });
            }

            return new LyricDto { Tracks = tracks };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static (LyricLine Line, bool IsBackground)? ParseLine(string line)
    {
        var attribute = AttributeRegex().Match(line);
        var isBackground = false;
        if (attribute.Success && attribute.Index == 0 && attribute.Length == 3)
        {
            var value = int.Parse(attribute.Groups[1].Value, CultureInfo.InvariantCulture);
            isBackground = value is < 0 or > 5;
            line = line[attribute.Length..];
        }

        var syllables = new List<LyricSyllable>();
        foreach (Match match in SyllableRegex().Matches(line))
        {
            if (!long.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var start)
                || !long.TryParse(match.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var duration)
                || start < 0 || duration < 0)
            {
                continue;
            }

            try
            {
                syllables.Add(new LyricSyllable
                {
                    Text = match.Groups[1].Value,
                    Start = checked(start * TimeSpan.TicksPerMillisecond),
                    End = checked((start + duration) * TimeSpan.TicksPerMillisecond)
                });
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        if (syllables.Count == 0)
        {
            return null;
        }

        var text = string.Concat(syllables.Select(i => i.Text));
        if (isBackground)
        {
            text = text.Trim('(', ')', '（', '）');
        }

        return (new LyricLine(text, syllables[0].Start)
        {
            End = syllables[^1].End,
            Syllables = syllables
        }, isBackground);
    }

    private static bool IsMetadata(string line)
    {
        var match = MetadataRegex().Match(line);
        return match.Success && _metadataTags.Contains(match.Groups[1].Value);
    }

    [GeneratedRegex(@"[A-Za-z]+\s*\(\d+,\d+\)")]
    private static partial Regex DetectionRegex();

    [GeneratedRegex(@"^\[(\d+)\]")]
    private static partial Regex AttributeRegex();

    [GeneratedRegex(@"(.*?)\((\d+),(\d+)\)")]
    private static partial Regex SyllableRegex();

    [GeneratedRegex(@"^\[([A-Za-z]+):\s*(.*)\]\s*$")]
    private static partial Regex MetadataRegex();
}
