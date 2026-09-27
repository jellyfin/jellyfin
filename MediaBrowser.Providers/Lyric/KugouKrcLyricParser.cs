using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Lyrics;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// Parser for Kugou KRC lyrics.
/// </summary>
public partial class KugouKrcLyricParser : ILyricParser
{
    private static readonly string[] _supportedMediaTypes = [".krc", ".qrc"];

    /// <inheritdoc />
    public string Name => "KugouKrcLyricProvider";

    /// <inheritdoc />
    public ResolverPriority Priority => ResolverPriority.Fourth;

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedExtensions => _supportedMediaTypes;

    /// <inheritdoc />
    public LyricDto? ParseLyrics(LyricFile lyrics)
    {
        if (!_supportedMediaTypes.Contains(Path.GetExtension(lyrics.Name.AsSpan()), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var lines = lyrics.Content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            var metadata = ParseMetadata(lines.FirstOrDefault(i => i.TrimStart().StartsWith("[language:", StringComparison.Ordinal)));
            var mainLines = new List<LyricLine>();
            var backgroundLines = new List<LyricLine>();
            var lineIndex = 0;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("[language:", StringComparison.Ordinal))
                {
                    continue;
                }

                if (line.StartsWith("[bg:", StringComparison.Ordinal))
                {
                    var background = ParseBackgroundLine(line);
                    if (background is not null)
                    {
                        backgroundLines.Add(background);
                    }

                    continue;
                }

                var match = KrcLineRegex().Match(line);
                if (!match.Success || !TryMilliseconds(match.Groups[1].Value, out var lineStart)
                    || !TryMilliseconds(match.Groups[2].Value, out var lineDuration))
                {
                    continue;
                }

                var syllables = ParseSyllables(match.Groups[3].Value, lineStart);
                if (syllables.Count == 0)
                {
                    lineIndex++;
                    continue;
                }

                if (metadata.Phonetics.TryGetValue(lineIndex, out var phonetics) && phonetics.Count == syllables.Count)
                {
                    for (var i = 0; i < syllables.Count; i++)
                    {
                        syllables[i].Phonetic = phonetics[i];
                    }
                }

                if (!TryAdd(lineStart, lineDuration, out var end))
                {
                    continue;
                }

                var text = string.Concat(syllables.Select(i => i.Text));
                var mainLine = new LyricLine(text, lineStart)
                {
                    End = end,
                    Syllables = syllables
                };
                mainLines.Add(mainLine);

                if (metadata.Translations.TryGetValue(lineIndex, out var translation))
                {
                    metadata.TranslationLines.Add(new LyricLine(translation, lineStart) { End = end });
                }

                lineIndex++;
            }

            if (mainLines.Count == 0)
            {
                return null;
            }

            var tracks = new List<LyricTrack>
            {
                new() { Type = LyricTrackType.Main, Lines = mainLines }
            };
            if (metadata.TranslationLines.Count > 0)
            {
                tracks.Add(new LyricTrack { Type = LyricTrackType.Translation, Lines = metadata.TranslationLines });
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

    private static LyricLine? ParseBackgroundLine(string line)
    {
        var match = BackgroundLineRegex().Match(line);
        if (!match.Success)
        {
            return null;
        }

        var syllables = MergeColonSyllables(ParseSyllables(match.Groups[1].Value, 0));
        if (syllables.Count == 0)
        {
            return null;
        }

        var text = string.Concat(syllables.Select(i => i.Text)).Trim();
        return new LyricLine(text.Trim('(', ')', '（', '）'), syllables[0].Start)
        {
            End = syllables[^1].End,
            Syllables = syllables
        };
    }

    private static List<LyricSyllable> ParseSyllables(string content, long baseStart)
    {
        var matches = SyllableRegex().Matches(content);
        var result = new List<LyricSyllable>();
        foreach (Match match in matches)
        {
            if (!TryMilliseconds(match.Groups[1].Value, out var offset)
                || !TryMilliseconds(match.Groups[2].Value, out var duration))
            {
                continue;
            }

            var textStart = match.Index + match.Length;
            var next = match.Index + match.Length < content.Length
                ? SyllableRegex().Match(content, textStart)
                : Match.Empty;
            var textEnd = next.Success ? next.Index : content.Length;
            var text = content[textStart..textEnd];
            if (text.Length == 0)
            {
                continue;
            }

            if (!TryAdd(baseStart, offset, out var start) || !TryAdd(start, duration, out var end))
            {
                continue;
            }

            result.Add(new LyricSyllable { Text = text, Start = start, End = end });
        }

        return MergeColonSyllables(result);
    }

    private static List<LyricSyllable> MergeColonSyllables(List<LyricSyllable> syllables)
    {
        var result = new List<LyricSyllable>(syllables.Count);
        for (var i = 0; i < syllables.Count; i++)
        {
            var current = syllables[i];
            if (i + 1 < syllables.Count && (syllables[i + 1].Text is ":" or "："))
            {
                var colon = syllables[++i];
                current.Text += colon.Text;
                current.End = colon.End;
            }

            result.Add(current);
        }

        return result;
    }

    private static bool TryAdd(long first, long second, out long result)
    {
        try
        {
            result = checked(first + second);
            return true;
        }
        catch (OverflowException)
        {
            result = 0;
            return false;
        }
    }

    private static KrcMetadata ParseMetadata(string? languageLine)
    {
        var metadata = new KrcMetadata();
        if (languageLine is null)
        {
            return metadata;
        }

        try
        {
            var encoded = languageLine[(languageLine.IndexOf(':', StringComparison.Ordinal) + 1)..].Trim().TrimEnd(']');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("content", out var content))
            {
                return metadata;
            }

            foreach (var item in content.EnumerateArray())
            {
                var type = item.TryGetProperty("type", out var typeValue) && typeValue.TryGetInt32(out var parsedType)
                    ? parsedType
                    : -1;
                if (!item.TryGetProperty("lyricContent", out var rows))
                {
                    continue;
                }

                foreach (var row in rows.EnumerateArray())
                {
                    if (type == 1)
                    {
                        metadata.Translations[metadata.Translations.Count] = string.Concat(row.EnumerateArray().Select(i => i.GetString() ?? string.Empty));
                    }
                    else if (type == 0)
                    {
                        metadata.Phonetics[metadata.Phonetics.Count] = row.EnumerateArray()
                            .Select(i => string.Concat(i.EnumerateArray().Select(j => j.GetString() ?? string.Empty)))
                            .ToList();
                    }
                }
            }
        }
        catch (Exception)
        {
            // Optional metadata must not invalidate the timed lyrics.
        }

        return metadata;
    }

    private static bool TryMilliseconds(string value, out long ticks)
    {
        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds) || milliseconds < 0)
        {
            ticks = 0;
            return false;
        }

        try
        {
            ticks = checked(milliseconds * TimeSpan.TicksPerMillisecond);
            return true;
        }
        catch (OverflowException)
        {
            ticks = 0;
            return false;
        }
    }

    [GeneratedRegex(@"^\[(\d+),(\d+)\](.*)$")]
    private static partial Regex KrcLineRegex();

    [GeneratedRegex(@"^\[bg:(.*)\](.*)$")]
    private static partial Regex BackgroundLineRegex();

    [GeneratedRegex(@"<(\d+),(\d+),\d+>")]
    private static partial Regex SyllableRegex();

    private sealed class KrcMetadata
    {
        public Dictionary<int, string> Translations { get; } = [];

        public Dictionary<int, IReadOnlyList<string>> Phonetics { get; } = [];

        public List<LyricLine> TranslationLines { get; } = [];
    }
}
