using System;
using System.Collections.Generic;
using System.Globalization;
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
/// TTML lyric parser.
/// </summary>
public partial class TtmlLyricParser : ILyricParser
{
    private static readonly string[] _supportedMediaTypes = [".ttml"];

    /// <inheritdoc />
    public string Name => "TtmlLyricProvider";

    /// <inheritdoc />
    public ResolverPriority Priority => ResolverPriority.Third;

    /// <inheritdoc />
    public LyricDto? ParseLyrics(LyricFile lyrics)
    {
        if (!_supportedMediaTypes.Contains(Path.GetExtension(lyrics.Name.AsSpan()), StringComparison.OrdinalIgnoreCase)
            && !lyrics.Content.Contains("http://www.w3.org/ns/ttml", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(PreformatTtml(lyrics.Content), LoadOptions.PreserveWhitespace);
        }
        catch (Exception)
        {
            return null;
        }

        var root = document.Root;
        if (root is null || !root.Name.LocalName.Equals("tt", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var artists = ParseArtists(document);
        var translations = ParseITunesTextMap(document, "translation");
        var transliterations = ParseITunesTransliterations(document);

        var mainLines = new List<LyricLine>();
        var translationLines = new List<LyricLine>();
        var phoneticLines = new List<LyricLine>();
        var translationLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var phoneticLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in document.Descendants().Where(i => i.Name.LocalName == "p"))
        {
            var start = ParseTime(GetAttributeValue(p, "begin"));
            var end = ParseTime(GetAttributeValue(p, "end"));
            if (!start.HasValue || !end.HasValue)
            {
                continue;
            }

            var artistIds = GetArtistIds(p);
            var key = GetAttributeValue(p, "key");
            var syllables = ParseSyllablesFromChildren(p.Nodes());
            var text = syllables.Count > 0 ? string.Concat(syllables.Select(i => i.Text)).Trim() : ExtractLineText(p).Trim();
            if (text.Length == 0)
            {
                continue;
            }

            if (key is not null && transliterations.TryGetValue(key, out var phonetics) && phonetics.Count == syllables.Count)
            {
                for (var i = 0; i < syllables.Count; i++)
                {
                    syllables[i].Phonetic = phonetics[i];
                }
            }

            mainLines.Add(new LyricLine(text, start)
            {
                End = end,
                ArtistIds = artistIds,
                Syllables = syllables
            });

            AddInlineTrackLine(p, "x-translation", translationLines, translationLanguages, start.Value, end.Value, artistIds);
            if (key is not null && translations.TryGetValue(key, out var externalTranslation))
            {
                translationLines.Add(new LyricLine(externalTranslation, start)
                {
                    End = end,
                    ArtistIds = artistIds
                });
            }

            AddInlineTrackLine(p, "x-roman", phoneticLines, phoneticLanguages, start.Value, end.Value, artistIds);

            foreach (var backgroundSpan in p.Elements().Where(i => HasRole(i, "x-bg")))
            {
                var backgroundStart = ParseTime(GetAttributeValue(backgroundSpan, "begin")) ?? start.Value;
                var backgroundEnd = ParseTime(GetAttributeValue(backgroundSpan, "end")) ?? end.Value;
                var backgroundSyllables = ParseSyllablesFromChildren(backgroundSpan.Nodes());
                var backgroundText = backgroundSyllables.Count > 0
                    ? string.Concat(backgroundSyllables.Select(i => i.Text)).Trim()
                    : ExtractLineText(backgroundSpan).Trim();
                if (backgroundText.Length == 0)
                {
                    continue;
                }

                mainLines.Add(new LyricLine(backgroundText, backgroundStart)
                {
                    End = backgroundEnd,
                    ArtistIds = artistIds,
                    Syllables = backgroundSyllables
                });

                AddInlineTrackLine(backgroundSpan, "x-translation", translationLines, translationLanguages, backgroundStart, backgroundEnd, artistIds);
            }
        }

        if (mainLines.Count == 0)
        {
            return null;
        }

        var tracks = new List<LyricTrack>
        {
            new()
            {
                Type = LyricTrackType.Main,
                Lines = mainLines.OrderBy(i => i.Start).ToArray()
            }
        };

        AddTrack(tracks, LyricTrackType.Translation, translationLines, translationLanguages);
        AddTrack(tracks, LyricTrackType.Phonetic, phoneticLines, phoneticLanguages);
        return new LyricDto
        {
            Metadata = new LyricMetadata
            {
                Artists = artists
            },
            Tracks = tracks
        };
    }

    private static void AddTrack(List<LyricTrack> tracks, LyricTrackType type, List<LyricLine> lines, IReadOnlyCollection<string> languages)
    {
        if (lines.Count == 0)
        {
            return;
        }

        tracks.Add(new LyricTrack
        {
            Type = type,
            Language = languages.Count == 1 ? languages.First() : null,
            Lines = lines.OrderBy(i => i.Start).ToArray()
        });
    }

    private static IReadOnlyList<Artist> ParseArtists(XDocument document)
    {
        return document.Descendants()
            .Where(i => i.Name.LocalName == "agent")
            .Select((agent, index) =>
            {
                var id = GetAttributeValue(agent, "id") ?? $"artist-{index + 1}";
                return new Artist
                {
                    Id = id,
                    Type = GetAttributeValue(agent, "type") ?? string.Empty,
                    Name = agent.Value.Trim()
                };
            })
            .ToArray();
    }

    private static Dictionary<string, string> ParseITunesTextMap(XDocument document, string containerName)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var container in document.Descendants().Where(i => i.Name.LocalName.Equals(containerName, StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var text in container.Descendants().Where(i => i.Name.LocalName == "text"))
            {
                var key = GetAttributeValue(text, "for");
                if (key is not null && !string.IsNullOrWhiteSpace(text.Value))
                {
                    result[key] = text.Value.Trim();
                }
            }
        }

        return result;
    }

    private static Dictionary<string, IReadOnlyList<string>> ParseITunesTransliterations(XDocument document)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var transliteration in document.Descendants().Where(i => i.Name.LocalName.Equals("transliteration", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var text in transliteration.Elements().Where(i => i.Name.LocalName == "text"))
            {
                var key = GetAttributeValue(text, "for");
                var phonetics = text.Elements()
                    .Where(i => i.Name.LocalName == "span")
                    .Select(i => i.Value.Trim())
                    .Where(i => i.Length > 0)
                    .ToArray();

                if (key is not null && phonetics.Length > 0)
                {
                    result[key] = phonetics;
                }
            }
        }

        return result;
    }

    private static void AddInlineTrackLine(
        XElement parent,
        string role,
        List<LyricLine> lines,
        ISet<string> languages,
        long start,
        long end,
        IReadOnlyList<string> artistIds)
    {
        foreach (var span in parent.Elements().Where(i => HasRole(i, role) && !HasRole(i, "x-bg")))
        {
            var text = span.Value.Trim();
            if (text.Length == 0)
            {
                continue;
            }

            var language = GetAttributeValue(span, "lang");
            if (!string.IsNullOrWhiteSpace(language))
            {
                languages.Add(language);
            }

            lines.Add(new LyricLine(text, start)
            {
                End = end,
                ArtistIds = artistIds
            });
        }
    }

    private static List<LyricSyllable> ParseSyllablesFromChildren(IEnumerable<XNode> nodes)
    {
        var nodeList = nodes.ToList();
        var syllables = new List<LyricSyllable>();
        for (var i = 0; i < nodeList.Count; i++)
        {
            if (nodeList[i] is not XElement span
                || span.Name.LocalName != "span"
                || HasAnyRole(span, "x-translation", "x-bg", "x-roman"))
            {
                continue;
            }

            var start = ParseTime(GetAttributeValue(span, "begin"));
            var end = ParseTime(GetAttributeValue(span, "end"));
            if (!start.HasValue || !end.HasValue || string.IsNullOrEmpty(span.Value))
            {
                continue;
            }

            var syllableText = span.Value;
            if (i + 1 < nodeList.Count && nodeList[i + 1] is XText nextText)
            {
                syllableText += nextText.Value;
            }

            syllables.Add(new LyricSyllable
            {
                Text = syllableText,
                Start = start.Value,
                End = end.Value
            });
        }

        if (syllables.Count > 0)
        {
            syllables[^1].Text = syllables[^1].Text.TrimEnd();
        }

        return syllables;
    }

    private static string ExtractLineText(XElement element)
    {
        var text = new List<string>();
        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XText xText:
                    text.Add(xText.Value);
                    break;
                case XElement child when !HasAnyRole(child, "x-translation", "x-bg", "x-roman"):
                    text.Add(child.Value);
                    break;
            }
        }

        return string.Concat(text);
    }

    private static IReadOnlyList<string> GetArtistIds(XElement element)
    {
        var agentId = GetAttributeValue(element, "agent");
        return string.IsNullOrWhiteSpace(agentId) ? [] : [agentId];
    }

    private static string? GetAttributeValue(XElement element, string localName)
        => element.Attributes().FirstOrDefault(i => i.Name.LocalName == localName)?.Value;

    private static bool HasRole(XElement element, string role)
        => element.Attributes().Any(i => i.Name.LocalName == "role" && i.Value == role);

    private static bool HasAnyRole(XElement element, params string[] roles)
        => element.Attributes().Any(i => i.Name.LocalName == "role" && roles.Contains(i.Value, StringComparer.Ordinal));

    private static string PreformatTtml(string content)
        => content
            .Replace("  ", string.Empty, StringComparison.Ordinal)
            .Replace(" </span><span", "</span> <span", StringComparison.Ordinal)
            .Replace(",</span><span", ",</span> <span", StringComparison.Ordinal);

    private static long? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var match = TtmlTimeRegex().Match(value.Trim());
        if (!match.Success)
        {
            return null;
        }

        var hours = match.Groups["h"].Success ? int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture) : 0;
        var minutes = match.Groups["m"].Success ? int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture) : 0;
        var seconds = int.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture);
        var fraction = match.Groups["f"].Success ? match.Groups["f"].Value : string.Empty;
        var ticks = new TimeSpan(hours, minutes, seconds).Ticks;
        if (fraction.Length > 0)
        {
            var paddedFraction = fraction.PadRight(7, '0')[..7];
            ticks += long.Parse(paddedFraction, CultureInfo.InvariantCulture);
        }

        return ticks;
    }

    [GeneratedRegex(@"^(?:(?<h>\d{1,2}):)?(?<m>\d{1,2}):(?<s>\d{1,2})(?:\.(?<f>\d{1,7}))?$")]
    private static partial Regex TtmlTimeRegex();
}
