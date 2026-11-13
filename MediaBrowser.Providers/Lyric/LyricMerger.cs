using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MediaBrowser.Model.Lyrics;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// Combines complementary lyric sources without replacing the best timing.
/// </summary>
internal static partial class LyricMerger
{
    private sealed record Source(LyricDto Data, LyricDto Original, Dictionary<LyricLine, LyricLine>? Pairs);

    internal static LyricDto? Merge(IEnumerable<LyricDto> sources)
    {
        var candidates = Prepare(sources.Where(s => Main(s).Count > 0))
            .OrderByDescending(s => Precision(Main(s.Data)))
            .ThenByDescending(s => Main(s.Data).Count(l => l.Syllables.Count > 0) / (double)Main(s.Data).Count)
            .ThenByDescending(s => s.Data.Metadata.Artists.Count + (s.Data.Tracks.First(t => t.Type == LyricTrackType.Main).Language is null ? 0 : 1))
            .ThenByDescending(s => Main(s.Data).Count(l => l.Start.HasValue))
            .ToArray();
        if (candidates.Length == 0)
        {
            return null;
        }

        var primary = candidates[0].Data;
        var main = Main(primary);
        var tracks = primary.Tracks.Select(t => new LyricTrack { Type = t.Type, Language = t.Language, Lines = t.Lines.ToArray() }).ToList();
        foreach (var entry in candidates.Skip(1))
        {
            var source = entry.Data;
            var otherMain = Main(source);
            var alignment = Align(main, otherMain);
            foreach (var otherTrack in source.Tracks.Where(t => t.Type is LyricTrackType.Translation or LyricTrackType.Phonetic))
            {
                var target = tracks.FirstOrDefault(t => t.Type == otherTrack.Type && SameLanguage(t.Language, otherTrack.Language));
                if (target is null && otherTrack.Language is null)
                {
                    // Shared translation text can identify the same track even
                    // when one source supplies no language tag. Never guess a
                    // language from its alphabet.
                    var texts = otherTrack.Lines.Select(l => Normalize(l.Text)).Where(t => t.Length >= 7).ToHashSet();
                    var matching = texts.Count < 2 ? [] : tracks.Where(t => t.Type == otherTrack.Type
                        && t.Lines.Count(l => texts.Contains(Normalize(l.Text))) >= 2).ToArray();
                    if (matching.Length == 1) target = matching[0];
                }
                if (target is null)
                {
                    target = new LyricTrack { Type = otherTrack.Type, Language = otherTrack.Language };
                    tracks.Add(target);
                }

                var lines = target.Lines.ToList();
                var positions = target.Lines.Select((line, i) => (line, index: FindLine(main, line, target.Lines.Count == main.Count ? i : -1)))
                    .ToDictionary(pair => pair.line, pair => pair.index);
                var covered = positions.Values.Where(i => i >= 0).ToHashSet();
                for (var i = 0; i < otherTrack.Lines.Count; i++)
                {
                    var line = otherTrack.Lines[i];
                    var sourceIndex = entry.Pairs is not null && entry.Pairs.TryGetValue(line, out var original)
                        ? IndexOf(otherMain, original)
                        : FindLine(otherMain, line, otherTrack.Lines.Count == otherMain.Count ? i : -1);
                    if (sourceIndex < 0 || !alignment.TryGetValue(sourceIndex, out var mainIndex) || !covered.Add(mainIndex))
                    {
                        continue;
                    }

                    var anchor = main[mainIndex];
                    var added = new LyricLine(line.Text, anchor.Start)
                    {
                        End = anchor.End,
                        ArtistIds = anchor.ArtistIds
                    };
                    lines.Add(added);
                    positions[added] = mainIndex;
                }

                // Sorting by base-line position also preserves order for unsynced lyrics.
                target.Lines = lines.OrderBy(l => positions.GetValueOrDefault(l, main.Count)).ToArray();
            }
        }

        return new LyricDto { Metadata = primary.Metadata, Tracks = tracks.Where(t => t.Lines.Count > 0).ToArray() };
    }

    private static Source[] Prepare(IEnumerable<LyricDto> sources)
    {
        var entries = sources.Select(source =>
        {
            if (source.Tracks.Any(t => t.Type != LyricTrackType.Main)) return new Source(source, source, null);
            var split = SplitBilingual(Main(source));
            if (split.Count == 1) return new Source(source, source, null);
            split[0].Language = source.Tracks.First(t => t.Type == LyricTrackType.Main).Language;
            var raw = Main(source).Where(l => !string.IsNullOrWhiteSpace(l.Text)).ToArray();
            var pairs = new Dictionary<LyricLine, LyricLine>();
            foreach (var translated in split[1].Lines)
            {
                var index = Array.IndexOf(raw, translated);
                if (index > 0) pairs[translated] = raw[index - 1];
            }

            return new Source(new LyricDto { Metadata = source.Metadata, Tracks = split }, source, pairs);
        }).ToArray();
        var anchors = entries.Where(e => e.Pairs is null).ToArray();
        return entries.Select(entry =>
        {
            if (entry.Pairs is null) return entry;
            var first = Main(entry.Data);
            var second = entry.Data.Tracks[1].Lines;
            var forward = anchors.Select(a => Align(Main(a.Data), first).Count).DefaultIfEmpty().Max();
            var reverse = anchors.Select(a => Align(Main(a.Data), second).Count).DefaultIfEmpty().Max();
            if (reverse >= 2 && reverse > forward)
            {
                var originals = entry.Pairs.Values.ToHashSet();
                var raw = Main(entry.Original).Where(l => !string.IsNullOrWhiteSpace(l.Text)).ToArray();
                return new Source(new LyricDto
                {
                    Metadata = entry.Data.Metadata,
                    Tracks =
                    [
                        new LyricTrack { Type = LyricTrackType.Main, Lines = raw.Where(l => !originals.Contains(l)).ToArray() },
                        new LyricTrack { Type = LyricTrackType.Translation, Lines = raw.Where(originals.Contains).ToArray() }
                    ]
                }, entry.Original, entry.Pairs.ToDictionary(p => p.Value, p => p.Key));
            }

            // Alternating plain text by itself is ambiguous. Require matching
            // originals from another source before interpreting it as translation.
            return !first.Any(l => l.Start.HasValue) && forward < 2
                ? new Source(entry.Original, entry.Original, null) : entry;
        }).ToArray();
    }

    private static int IndexOf(IReadOnlyList<LyricLine> lines, LyricLine value)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            if (ReferenceEquals(lines[i], value)) return i;
        }

        return -1;
    }

    internal static IReadOnlyList<LyricTrack> SplitBilingual(IReadOnlyList<LyricLine> lines)
    {
        var groups = lines.Any(l => l.Start.HasValue)
            ? lines.GroupBy(l => l.Start).Select(g => g.ToArray()).ToArray()
            : lines.Where(l => !string.IsNullOrWhiteSpace(l.Text)).Chunk(2).ToArray();
        var pairs = groups.Where(g => g.Length == 2
            && !string.IsNullOrWhiteSpace(g[0].Text) && !string.IsNullOrWhiteSpace(g[1].Text)
            && Normalize(g[0].Text) != Normalize(g[1].Text)
            && !(g[0].Syllables.Count > 0 && g[1].Syllables.Count > 0)
            && !(g[0].ArtistIds.Count > 0 && g[1].ArtistIds.Count > 0 && !g[0].ArtistIds.Intersect(g[1].ArtistIds).Any())
            && !DifferentVoices(g[0].Text, g[1].Text)).ToArray();
        // Infer only a sustained two-row structure, never a language or script.
        // An independent source is used below to validate/reverse the original
        // column. Untimed pairs require that independent evidence.
        if (pairs.Length < 2 || pairs.Length * 2 < groups.Length)
        {
            return [new LyricTrack { Type = LyricTrackType.Main, Lines = lines }];
        }

        var translations = pairs.Select(g => g[1]).ToHashSet();
        return
        [
            new LyricTrack { Type = LyricTrackType.Main, Lines = lines.Where(l => !translations.Contains(l) && !string.IsNullOrWhiteSpace(l.Text)).ToArray() },
            new LyricTrack { Type = LyricTrackType.Translation, Lines = lines.Where(translations.Contains).ToArray() }
        ];
    }

    internal static string FileKey(string name)
        => Normalize(FeaturedArtistRegex().Replace(TrackNumberRegex().Replace(name, string.Empty), string.Empty));

    // 0 = unsynced, 1 = synced, 2 = karaoke. File extensions do not determine quality.
    private static int Precision(IReadOnlyList<LyricLine> lines)
        => lines.Any(l => l.Syllables.Count > 0) ? 2 : lines.Any(l => l.Start.HasValue) ? 1 : 0;

    private static IReadOnlyList<LyricLine> Main(LyricDto source)
        => source.Tracks.FirstOrDefault(t => t.Type == LyricTrackType.Main)?.Lines ?? [];

    private static bool SameLanguage(string? left, string? right)
    {
        static string Key(string? language)
        {
            var key = language?.ToLowerInvariant() ?? string.Empty;
            return key is "zh-cn" or "zh-sg" or "zh-hans-cn" ? "zh-hans" : key;
        }

        var a = Key(left);
        var b = Key(right);
        return a == b || (a.Length == 2 && b.StartsWith(a + "-", StringComparison.Ordinal))
            || (b.Length == 2 && a.StartsWith(b + "-", StringComparison.Ordinal));
    }

    private static int FindLine(IReadOnlyList<LyricLine> main, LyricLine line, int fallback)
    {
        if (line.Start.HasValue)
        {
            for (var i = 0; i < main.Count; i++)
            {
                if (main[i].Start == line.Start)
                {
                    return i;
                }
            }

            for (var i = 0; i < main.Count; i++)
            {
                if (main[i].Start <= line.Start && main[i].End > line.Start)
                {
                    return i;
                }
            }
        }

        return fallback;
    }

    internal static Dictionary<int, int> Align(IReadOnlyList<LyricLine> primary, IReadOnlyList<LyricLine> secondary)
    {
        var a = primary.Select(l => Normalize(l.Text)).ToArray();
        var b = secondary.Select(l => Normalize(l.Text)).ToArray();
        var result = new Dictionary<int, int>();
        if ((long)a.Length * b.Length > 1_000_000)
        {
            // Bound memory and CPU on unexpectedly large inputs. Exact matches
            // still follow source order; unmatched text is never guessed.
            var position = 0;
            for (var j = 0; j < b.Length; j++)
            {
                var match = Array.FindIndex(a, position, text => text.Length > 0 && text == b[j]);
                if (match >= 0)
                {
                    result[j] = match;
                    position = match + 1;
                }
            }

            return result;
        }

        var scores = new double[a.Length + 1, b.Length + 1];
        var choices = new byte[a.Length + 1, b.Length + 1];
        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var up = scores[i - 1, j];
                var left = scores[i, j - 1];
                scores[i, j] = Math.Max(up, left);
                choices[i, j] = up >= left ? (byte)1 : (byte)2;
                var similarity = Similarity(a[i - 1], b[j - 1]);
                if (similarity == 0)
                {
                    continue;
                }

                var match = scores[i - 1, j - 1] + similarity;
                if (match >= scores[i, j])
                {
                    scores[i, j] = match;
                    choices[i, j] = 3;
                }
            }
        }

        for (int i = a.Length, j = b.Length; i > 0 && j > 0;)
        {
            switch (choices[i, j])
            {
                case 3:
                    result[--j] = --i;
                    break;
                case 1:
                    i--;
                    break;
                default:
                    j--;
                    break;
            }
        }

        return result;
    }

    private static double Similarity(string a, string b)
    {
        if (a.Length == 0 || b.Length == 0)
        {
            return 0;
        }

        if (a == b)
        {
            return 2;
        }

        var longest = Math.Max(a.Length, b.Length);
        var limit = (int)(longest * 0.14);
        if (Math.Min(a.Length, b.Length) < 7 || longest > 512 || Math.Abs(a.Length - b.Length) > limit)
        {
            return 0;
        }

        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        var current = new int[b.Length + 1];
        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            var minimum = i;
            for (var j = 1; j <= b.Length; j++)
            {
                current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
                minimum = Math.Min(minimum, current[j]);
            }

            if (minimum > limit)
            {
                return 0;
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length] <= limit ? 1 - (previous[b.Length] / (double)longest) : 0;
    }

    internal static string Normalize(string text)
    {
        var normalized = VoicePrefixRegex().Replace(text, string.Empty).Normalize(NormalizationForm.FormKC);
        var result = new StringBuilder(normalized.Length);
        foreach (var rune in normalized.EnumerateRunes())
        {
            if (Rune.IsLetterOrDigit(rune))
            {
                result.Append(Rune.ToLowerInvariant(rune));
            }
        }

        return result.ToString();
    }

    private static bool DifferentVoices(string left, string right)
    {
        var first = VoicePrefixRegex().Match(left);
        var second = VoicePrefixRegex().Match(right);
        return first.Success && second.Success && !first.Value.Trim().Equals(second.Value.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"^\s*v\d+\s*:\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VoicePrefixRegex();

    [GeneratedRegex(@"^\s*\d{1,3}(?:[-.]\d{1,3})?[. _-]+", RegexOptions.CultureInvariant)]
    private static partial Regex TrackNumberRegex();

    [GeneratedRegex(@"\s*[\(\[]feat\.?\s+[^\)\]]+[\)\]]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FeaturedArtistRegex();
}
