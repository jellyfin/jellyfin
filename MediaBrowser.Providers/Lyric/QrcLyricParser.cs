using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Lyrics;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// Parser for QQ Music QRC lyrics.
/// </summary>
public partial class QrcLyricParser : ILyricParser
{
    private static readonly string[] _supportedMediaTypes = [".qrc"];

    /// <inheritdoc />
    public string Name => "QrcLyricProvider";

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
            var content = ExtractQrcContent(lyrics.Content);
            var offset = TimedLyricParserHelpers.ParseOffset(content);
            var result = new List<LyricLine>();
            foreach (var rawLine in TimedLyricParserHelpers.SplitLines(content, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                var match = LineRegex().Match(line);
                if (!match.Success || !TimedLyricParserHelpers.TryMilliseconds(match.Groups[1].Value, out var lineStart)
                    || !TimedLyricParserHelpers.TryMilliseconds(match.Groups[2].Value, out var lineDuration))
                {
                    continue;
                }

                if (!TimedLyricParserHelpers.TryApplyOffset(lineStart, offset, out lineStart)
                    || !TimedLyricParserHelpers.TryAdd(lineStart, lineDuration, out var lineEnd))
                {
                    continue;
                }

                var syllables = TimedLyricParserHelpers.ParsePostfixSyllables(match.Groups[3].Value, offset, out var hasTiming);
                if (syllables.Count == 0)
                {
                    var plainText = match.Groups[3].Value.Trim();
                    if (!hasTiming && plainText.Length > 0)
                    {
                        result.Add(new LyricLine(plainText, lineStart) { End = lineEnd });
                    }

                    continue;
                }

                lineEnd = Math.Max(lineEnd, syllables.Max(i => i.End ?? lineEnd));
                result.Add(new LyricLine(string.Concat(syllables.Select(i => i.Text)), lineStart)
                {
                    End = lineEnd,
                    Syllables = syllables
                });
            }

            return result.Count == 0
                ? null
                : new LyricDto
                {
                    Tracks = [new LyricTrack { Type = LyricTrackType.Main, Lines = result }]
                };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string ExtractQrcContent(string content)
    {
        if (!content.TrimStart().StartsWith('<'))
        {
            return content;
        }

        var document = XDocument.Parse(content);
        return document.Descendants().Attributes("LyricContent").FirstOrDefault()?.Value ?? string.Empty;
    }

    [GeneratedRegex(@"^\[(\d+),\s*(\d+)\](.*)$")]
    private static partial Regex LineRegex();
}
