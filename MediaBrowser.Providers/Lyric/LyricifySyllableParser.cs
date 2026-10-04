using System;
using System.Collections.Generic;
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
    private static readonly string[] _supportedMediaTypes = [".lys"];
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
        if (!_supportedMediaTypes.Contains(Path.GetExtension(lyrics.Name.AsSpan()), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var mainLines = new List<LyricLine>();
            var backgroundLines = new List<LyricLine>();
            var offset = TimedLyricParserHelpers.ParseOffset(lyrics.Content);
            foreach (var rawLine in TimedLyricParserHelpers.SplitLines(lyrics.Content, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || IsMetadata(line))
                {
                    continue;
                }

                var parsed = ParseLine(line, offset);
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

    private static (LyricLine Line, bool IsBackground)? ParseLine(string line, long offset)
    {
        var attribute = AttributeRegex().Match(line);
        var isBackground = false;
        if (attribute.Success)
        {
            if (!int.TryParse(attribute.Groups[1].Value, out var value) || value > 8)
            {
                return null;
            }

            isBackground = value > 5;
            line = line[attribute.Length..];
        }

        var syllables = TimedLyricParserHelpers.ParsePostfixSyllables(line, offset, out _);
        if (syllables.Count == 0)
        {
            return null;
        }

        if (isBackground)
        {
            TimedLyricParserHelpers.StripBackgroundParentheses(syllables);
        }

        var text = string.Concat(syllables.Select(i => i.Text));
        return (new LyricLine(text, syllables[0].Start)
        {
            End = syllables.Max(i => i.End),
            Syllables = syllables
        }, isBackground);
    }

    private static bool IsMetadata(string line)
    {
        var match = MetadataRegex().Match(line);
        return match.Success && _metadataTags.Contains(match.Groups[1].Value);
    }

    [GeneratedRegex(@"^\[(\d+)\]")]
    private static partial Regex AttributeRegex();

    [GeneratedRegex(@"^\[([A-Za-z]+):\s*(.*)\]\s*$")]
    private static partial Regex MetadataRegex();
}
