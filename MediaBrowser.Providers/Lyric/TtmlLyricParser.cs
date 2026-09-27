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
    private static readonly XNamespace _ttmlNamespace = "http://www.w3.org/ns/ttml";
    private static readonly XNamespace _ttmNamespace = "http://www.w3.org/ns/ttml#metadata";
    private static readonly string[] _supportedMediaTypes = [".ttml"];

    /// <inheritdoc />
    public string Name => "TtmlLyricProvider";

    /// <inheritdoc />
    public ResolverPriority Priority => ResolverPriority.Third;

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedExtensions => _supportedMediaTypes;

    /// <summary>
    /// Gets or sets a value indicating whether Apple TTML restrictions are enforced.
    /// </summary>
    /// <remarks>
    /// The default is <see langword="false"/> to preserve compatibility with existing lyric files.
    /// </remarks>
    public bool StrictValidation { get; set; }

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
            document = XDocument.Parse(lyrics.Content, LoadOptions.PreserveWhitespace);
        }
        catch (Exception)
        {
            return null;
        }

        if (StrictValidation
            && (lyrics.Content.StartsWith('\uFEFF')
                || document.Declaration is null
                || !string.Equals(document.Declaration.Encoding, "UTF-8", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var root = document.Root;
        if (root is null || !root.Name.LocalName.Equals("tt", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (StrictValidation && !ValidateTtml(root))
        {
            return null;
        }

        var body = root.Element(_ttmlNamespace + "body");
        var duration = body is null ? null : ParseTime(GetAttributeValue(body, "dur"));

        var artists = ParseArtists(document);
        var translations = ParseITunesTextMap(document, "translation");
        var transliterations = ParseITunesTransliterations(document);

        var mainLines = new List<LyricLine>();
        var translationLines = new Dictionary<string, List<LyricLine>>(StringComparer.OrdinalIgnoreCase);
        var phoneticLines = new Dictionary<string, List<LyricLine>>(StringComparer.OrdinalIgnoreCase);
        var backgroundLines = new List<LyricLine>();

        foreach (var p in document.Descendants().Where(i => i.Name.LocalName == "p"))
        {
            var start = ParseTime(GetAttributeValue(p, "begin"));
            var end = ParseTime(GetAttributeValue(p, "end"));
            var artistIds = p.AncestorsAndSelf()
                .SelectMany(GetArtistIds)
                .Concat(p.Descendants(_ttmlNamespace + "span").SelectMany(GetArtistIds))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var key = GetAttributeValue(p, "key");
            var syllables = ParseSyllablesFromChildren(p.Nodes());
            var text = syllables.Count > 0
                ? string.Concat(syllables.Select(i => i.Text)).Trim()
                : NormalizeXmlTextContent(ExtractLineText(p));
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

            AddInlineTrackLine(p, "x-translation", translationLines, start, end, artistIds);
            if (key is not null && translations.TryGetValue(key, out var externalTranslation))
            {
                AddTrackLine(translationLines, string.Empty, new LyricLine(externalTranslation, start)
                {
                    End = end,
                    ArtistIds = artistIds
                });
            }

            AddInlineTrackLine(p, "x-roman", phoneticLines, start, end, artistIds);

            foreach (var backgroundSpan in p.Elements().Where(i => HasRole(i, "x-bg")))
            {
                var backgroundStart = ParseTime(GetAttributeValue(backgroundSpan, "begin")) ?? start;
                var backgroundEnd = ParseTime(GetAttributeValue(backgroundSpan, "end")) ?? end;
                var backgroundSyllables = ParseSyllablesFromChildren(backgroundSpan.Nodes());
                var backgroundText = backgroundSyllables.Count > 0
                    ? string.Concat(backgroundSyllables.Select(i => i.Text)).Trim()
                    : NormalizeXmlTextContent(ExtractLineText(backgroundSpan));
                if (backgroundText.Length == 0)
                {
                    continue;
                }

                backgroundLines.Add(new LyricLine(backgroundText, backgroundStart)
                {
                    End = backgroundEnd,
                    ArtistIds = artistIds,
                    Syllables = backgroundSyllables
                });

                AddInlineTrackLine(backgroundSpan, "x-translation", translationLines, backgroundStart, backgroundEnd, artistIds);
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

        AddTracks(tracks, LyricTrackType.Translation, translationLines);
        AddTracks(tracks, LyricTrackType.Phonetic, phoneticLines);
        if (backgroundLines.Count > 0)
        {
            tracks.Add(new LyricTrack
            {
                Type = LyricTrackType.Background,
                Lines = backgroundLines.OrderBy(i => i.Start).ToArray()
            });
        }

        return new LyricDto
        {
            Metadata = new LyricMetadata
            {
                Artists = artists,
                Duration = duration
            },
            Tracks = tracks
        };
    }

    private static bool ValidateTtml(XElement root)
    {
        if (root.Name.Namespace != _ttmlNamespace)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(root.Attribute(XNamespace.Xml + "lang")?.Value))
        {
            return false;
        }

        var heads = root.Elements(_ttmlNamespace + "head").ToArray();
        var metadata = heads.Length == 1 ? heads[0].Element(_ttmlNamespace + "metadata") : null;
        if (metadata?.Elements(_ttmNamespace + "title").Any(i => !string.IsNullOrWhiteSpace(i.Value)) != true)
        {
            return false;
        }

        var agents = metadata.Elements(_ttmNamespace + "agent").ToArray();
        var agentIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var agent in agents)
        {
            var id = GetXmlId(agent);
            var type = GetUnqualifiedAttributeValue(agent, "type");
            if (string.IsNullOrWhiteSpace(id)
                || !agentIds.Add(id)
                || type is not ("person" or "group" or "other"))
            {
                return false;
            }

            foreach (var name in agent.Elements(_ttmNamespace + "name"))
            {
                if (!string.Equals(GetUnqualifiedAttributeValue(name, "type"), "full", StringComparison.Ordinal))
                {
                    return false;
                }
            }
        }

        var bodies = root.Elements(_ttmlNamespace + "body").ToArray();
        if (bodies.Length != 1)
        {
            return false;
        }

        var body = bodies[0];
        if (body.Descendants(_ttmlNamespace + "div").Any(i => i.Parent != body)
            || body.Descendants(_ttmlNamespace + "p").Any(i => i.Parent is not XElement parent || parent.Name != _ttmlNamespace + "div"))
        {
            return false;
        }

        foreach (var element in root.Descendants())
        {
            if (element.Name == _ttmlNamespace + "br")
            {
                return false;
            }

            var agent = GetNamespacedAttributeValue(element, _ttmNamespace, "agent");
            if (agent is not null)
            {
                if (element.Name != _ttmlNamespace + "p"
                    && element.Name != _ttmlNamespace + "span")
                {
                    return false;
                }

                if (!agentIds.Contains(agent)
                    || element.Ancestors().Any(i => GetNamespacedAttributeValue(i, _ttmNamespace, "agent") is not null))
                {
                    return false;
                }
            }
        }

        if (!TryValidateTiming(body, null, out var bodyStart, out var bodyEnd))
        {
            return false;
        }

        var duration = GetUnqualifiedAttributeValue(body, "dur");
        var songDuration = ParseAppleTime(duration);
        if (!string.IsNullOrWhiteSpace(duration) && (!songDuration.HasValue || songDuration <= 0))
        {
            return false;
        }

        if (body.Elements(_ttmlNamespace + "div").Any() == false)
        {
            return false;
        }

        long? previousDivEnd = null;
        foreach (var div in body.Elements(_ttmlNamespace + "div"))
        {
            if (!TryValidateTiming(div, bodyStart, out var divStart, out var divEnd)
                || (divStart.HasValue && bodyEnd.HasValue && divEnd > bodyEnd)
                || !WithinDuration(divStart, divEnd, songDuration))
            {
                return false;
            }

            if (divStart.HasValue && previousDivEnd.HasValue && divStart < previousDivEnd)
            {
                return false;
            }

            previousDivEnd = divEnd ?? previousDivEnd;

            foreach (var paragraph in div.Elements(_ttmlNamespace + "p"))
            {
                var backgroundSpans = paragraph.Elements(_ttmlNamespace + "span")
                    .Where(i => GetNamespacedAttributeValue(i, _ttmNamespace, "role") == "x-bg")
                    .ToArray();
                if (backgroundSpans.Length > 0
                    && (paragraph.Elements().FirstOrDefault() != backgroundSpans[0]
                        && paragraph.Elements().LastOrDefault() != backgroundSpans[^1]))
                {
                    return false;
                }

                if (!TryValidateTiming(paragraph, divStart, out var paragraphStart, out var paragraphEnd)
                    || (paragraphStart.HasValue && divEnd.HasValue && paragraphEnd > divEnd)
                    || !WithinDuration(paragraphStart, paragraphEnd, songDuration))
                {
                    return false;
                }

                foreach (var span in paragraph.Descendants(_ttmlNamespace + "span"))
                {
                    if (!TryValidateTiming(span, paragraphStart, out var spanStart, out var spanEnd)
                        || (spanStart.HasValue && paragraphEnd.HasValue && spanEnd > paragraphEnd)
                        || !WithinDuration(spanStart, spanEnd, songDuration))
                    {
                        return false;
                    }
                }
            }
        }

        var intervalsByAgent = new Dictionary<string, List<(long Start, long End)>>(StringComparer.Ordinal);
        foreach (var element in body.Descendants().Where(i => i.Name == _ttmlNamespace + "p" || i.Name == _ttmlNamespace + "span"))
        {
            if (!TryValidateTiming(element, null, out var start, out var end) || !start.HasValue || !end.HasValue)
            {
                continue;
            }

            var agent = GetNamespacedAttributeValue(element, _ttmNamespace, "agent");
            if (agent is null)
            {
                continue;
            }

            if (!intervalsByAgent.TryGetValue(agent, out var intervals))
            {
                intervals = [];
                intervalsByAgent[agent] = intervals;
            }

            intervals.Add((start.Value, end.Value));
        }

        return intervalsByAgent.Values.All(intervals =>
        {
            intervals.Sort((left, right) => left.Start.CompareTo(right.Start));
            return intervals.Zip(intervals.Skip(1), (left, right) => right.Start >= left.End).All(i => i);
        });
    }

    private static bool WithinDuration(long? start, long? end, long? duration)
        => !duration.HasValue || !start.HasValue || !end.HasValue || end <= duration;

    private static bool TryValidateTiming(XElement element, long? parentStart, out long? start, out long? end)
    {
        var begin = GetUnqualifiedAttributeValue(element, "begin");
        var finish = GetUnqualifiedAttributeValue(element, "end");
        if (string.IsNullOrWhiteSpace(begin) != string.IsNullOrWhiteSpace(finish))
        {
            start = null;
            end = null;
            return false;
        }

        if (string.IsNullOrWhiteSpace(begin))
        {
            start = null;
            end = null;
            return true;
        }

        start = ParseAppleTime(begin);
        end = ParseAppleTime(finish);
        return start.HasValue
            && end.HasValue
            && start < end
            && (!parentStart.HasValue || start >= parentStart);
    }

    private static void AddTracks(List<LyricTrack> tracks, LyricTrackType type, IReadOnlyDictionary<string, List<LyricLine>> linesByLanguage)
    {
        foreach (var (language, lines) in linesByLanguage)
        {
            if (lines.Count == 0)
            {
                continue;
            }

            tracks.Add(new LyricTrack
            {
                Type = type,
                Language = language.Length == 0 ? null : language,
                Lines = lines.OrderBy(i => i.Start).ToArray()
            });
        }
    }

    private static void AddTrackLine(Dictionary<string, List<LyricLine>> linesByLanguage, string language, LyricLine line)
    {
        if (!linesByLanguage.TryGetValue(language, out var lines))
        {
            lines = [];
            linesByLanguage[language] = lines;
        }

        lines.Add(line);
    }

    private static IReadOnlyList<Artist> ParseArtists(XDocument document)
    {
        return document.Descendants()
            .Where(i => i.Name.LocalName == "agent")
            .Select((agent, index) =>
            {
                var id = GetXmlId(agent) ?? $"artist-{index + 1}";
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
        Dictionary<string, List<LyricLine>> linesByLanguage,
        long? start,
        long? end,
        IReadOnlyList<string> artistIds)
    {
        foreach (var span in parent.Elements().Where(i => HasRole(i, role) && !HasRole(i, "x-bg")))
        {
            var text = span.Value.Trim();
            if (text.Length == 0)
            {
                continue;
            }

            var language = GetAttributeValue(span, "lang") ?? string.Empty;
            AddTrackLine(linesByLanguage, language, new LyricLine(text, start)
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
                syllableText += NormalizeInterSyllableSpace(nextText.Value);
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

    private static string NormalizeXmlTextContent(string text)
        => WhitespaceCollapseRegex().Replace(text, " ").Trim();

    // Keep one word separator between karaoke spans. Drop pure XML indentation
    // (newlines with no space/tab before them); keep a single space when an
    // inline space sits before a pretty-print newline (`</span> \n<span>`).
    private static string NormalizeInterSyllableSpace(string rawText)
    {
        if (string.IsNullOrEmpty(rawText))
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(rawText))
        {
            return rawText;
        }

        var newlineIndex = rawText.IndexOfAny(['\n', '\r']);
        var inlinePrefix = newlineIndex < 0 ? rawText.AsSpan() : rawText.AsSpan(0, newlineIndex);
        var hasSemanticInlineSpace = inlinePrefix.Contains(' ') || inlinePrefix.Contains('\t');
        var isLayoutWhitespace = newlineIndex >= 0;

        if (!isLayoutWhitespace || hasSemanticInlineSpace)
        {
            return " ";
        }

        return string.Empty;
    }

    private static IReadOnlyList<string> GetArtistIds(XElement element)
    {
        var agentId = GetAttributeValue(element, "agent");
        return string.IsNullOrWhiteSpace(agentId) ? [] : [agentId];
    }

    private static string? GetAttributeValue(XElement element, string localName)
        => element.Attributes().FirstOrDefault(i => i.Name.LocalName == localName)?.Value;

    private static string? GetUnqualifiedAttributeValue(XElement element, string localName)
        => element.Attribute(localName)?.Value;

    private static string? GetNamespacedAttributeValue(XElement element, XNamespace @namespace, string localName)
        => element.Attribute(@namespace + localName)?.Value;

    private static string? GetXmlId(XElement element)
        => element.Attribute(XNamespace.Xml + "id")?.Value;

    private static bool HasRole(XElement element, string role)
        => element.Attributes().Any(i => i.Name.LocalName == "role" && i.Value == role);

    private static bool HasAnyRole(XElement element, params string[] roles)
        => element.Attributes().Any(i => i.Name.LocalName == "role" && roles.Contains(i.Value, StringComparer.Ordinal));

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

        try
        {
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
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or FormatException or OverflowException)
        {
            return null;
        }
    }

    private static long? ParseAppleTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var match = AppleTimeRegex().Match(value.Trim());
        if (!match.Success)
        {
            return null;
        }

        try
        {
            var hours = match.Groups["h"].Success ? int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture) : 0;
            var minutes = int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture);
            var seconds = int.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture);
            if (minutes > 59 || seconds > 59)
            {
                return null;
            }

            var ticks = new TimeSpan(hours, minutes, seconds).Ticks;
            var fraction = match.Groups["f"].Value;
            if (fraction.Length > 0)
            {
                ticks += long.Parse(fraction.PadRight(7, '0'), CultureInfo.InvariantCulture);
            }

            return ticks;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or FormatException or OverflowException)
        {
            return null;
        }
    }

    [GeneratedRegex(@"^(?:(?<h>\d{1,2}):)?(?<m>\d{1,2}):(?<s>\d{1,2})(?:\.(?<f>\d{1,7}))?$")]
    private static partial Regex TtmlTimeRegex();

    [GeneratedRegex(@"^(?:(?<h>\d+):)?(?<m>\d{2}):(?<s>\d{2})(?:\.(?<f>\d{1,3}))?$")]
    private static partial Regex AppleTimeRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceCollapseRegex();
}
