using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
    private static readonly XNamespace _ttsNamespace = "http://www.w3.org/ns/ttml#styling";
    private static readonly XNamespace _ttmNamespace = "http://www.w3.org/ns/ttml#metadata";
    private static readonly XNamespace _itunesNamespace = "http://itunes.apple.com/lyric-ttml-extensions";
    private static readonly XNamespace _itunesInternalNamespace = "http://music.apple.com/lyric-ttml-internal";
    private static readonly char[] _xmlWhitespaceCharacters = [' ', '\t', '\r', '\n'];
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
            && !_supportedMediaTypes.Contains(Path.GetExtension(lyrics.Name.AsSpan()), StringComparison.OrdinalIgnoreCase))
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

        var artists = ParseArtists(document, StrictValidation);
        var translations = ParseITunesTextMap(document, "translation", StrictValidation);
        var transliterations = ParseITunesTransliterations(document, StrictValidation);

        var mainLines = new List<LyricLine>();
        var translationLines = new Dictionary<string, List<LyricLine>>(StringComparer.OrdinalIgnoreCase);
        var phoneticLines = new Dictionary<string, List<LyricLine>>(StringComparer.OrdinalIgnoreCase);
        var backgroundLines = new List<LyricLine>();

        var paragraphs = StrictValidation
            ? document.Descendants(_ttmlNamespace + "p")
            : document.Descendants().Where(i => i.Name.LocalName == "p");

        foreach (var p in paragraphs)
        {
            ReadTiming(p, out var start, out var end);
            var artistIds = GetLineArtistIds(p, StrictValidation);
            var key = GetAttributeValue(p, "key");
            var syllables = ParseSyllablesFromChildren(p, StrictValidation);
            var text = syllables.Count > 0
                ? string.Concat(syllables.Select(i => i.Text))
                : NormalizeXmlTextContent(p, ExtractLineText(p, StrictValidation));

            if (text.Length > 0)
            {
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

                AddInlineTrackLine(p, "x-translation", translationLines, start, end, artistIds, StrictValidation);
                if (key is not null && translations.TryGetValue(key, out var externalTranslations))
                {
                    foreach (var externalTranslation in externalTranslations)
                    {
                        foreach (var translationText in externalTranslation.Value)
                        {
                            AddTrackLine(translationLines, externalTranslation.Key, new LyricLine(translationText, start)
                            {
                                End = end,
                                ArtistIds = artistIds
                            });
                        }
                    }
                }

                AddInlineTrackLine(p, "x-roman", phoneticLines, start, end, artistIds, StrictValidation);
            }

            foreach (var backgroundSpan in FindRoleSpans(p, "x-bg", StrictValidation))
            {
                ReadInheritedTiming(backgroundSpan, p, start, end, out var backgroundStart, out var backgroundEnd);
                var backgroundArtistIds = GetLineArtistIds(backgroundSpan, StrictValidation);
                var backgroundSyllables = ParseSyllablesFromChildren(backgroundSpan, StrictValidation);
                var backgroundText = backgroundSyllables.Count > 0
                    ? string.Concat(backgroundSyllables.Select(i => i.Text))
                    : NormalizeXmlTextContent(backgroundSpan, ExtractLineText(backgroundSpan, StrictValidation));
                if (backgroundText.Length == 0)
                {
                    continue;
                }

                backgroundLines.Add(new LyricLine(backgroundText, backgroundStart)
                {
                    End = backgroundEnd,
                    ArtistIds = backgroundArtistIds,
                    Syllables = backgroundSyllables
                });

                AddInlineTrackLine(backgroundSpan, "x-translation", translationLines, backgroundStart, backgroundEnd, backgroundArtistIds, StrictValidation);
                AddInlineTrackLine(backgroundSpan, "x-roman", phoneticLines, backgroundStart, backgroundEnd, backgroundArtistIds, StrictValidation);
            }
        }

        if (mainLines.Count == 0 && backgroundLines.Count == 0)
        {
            return null;
        }

        var tracks = new List<LyricTrack>();
        if (mainLines.Count > 0)
        {
            tracks.Add(new LyricTrack
            {
                Type = LyricTrackType.Main,
                Lines = mainLines.OrderBy(i => i.Start).ToArray()
            });
        }

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

        if (root.GetNamespaceOfPrefix("tts") != _ttsNamespace
            || root.GetNamespaceOfPrefix("itunes") != _itunesNamespace
            || root.GetNamespaceOfPrefix("ttm") != _ttmNamespace)
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

        if (!WithinDuration(bodyStart, bodyEnd, songDuration))
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
            var effectiveDivStart = divStart ?? bodyStart;
            var effectiveDivEnd = divEnd ?? bodyEnd;

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

                if (!TryValidateTiming(paragraph, effectiveDivStart, out var paragraphStart, out var paragraphEnd)
                    || (paragraphStart.HasValue && effectiveDivEnd.HasValue && paragraphEnd > effectiveDivEnd)
                    || !WithinDuration(paragraphStart, paragraphEnd, songDuration))
                {
                    return false;
                }

                if (!ValidateSpanTiming(paragraph, paragraphStart ?? effectiveDivStart, paragraphEnd ?? effectiveDivEnd, songDuration))
                {
                    return false;
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

    private static bool ValidateSpanTiming(XElement paragraph, long? start, long? end, long? duration)
    {
        var bounds = new Dictionary<XElement, (long? Start, long? End)>
        {
            [paragraph] = (start, end)
        };
        foreach (var span in paragraph.Descendants(_ttmlNamespace + "span"))
        {
            var parent = span.Parent;
            while (parent is not null && !bounds.ContainsKey(parent))
            {
                parent = parent.Parent;
            }

            var parentBounds = parent is null ? (Start: start, End: end) : bounds[parent];
            if (!TryValidateTiming(span, parentBounds.Start, out var spanStart, out var spanEnd)
                || (spanStart.HasValue && parentBounds.End.HasValue && spanEnd > parentBounds.End)
                || !WithinDuration(spanStart, spanEnd, duration))
            {
                return false;
            }

            // An untimed wrapper inherits its parent's constraints for descendants.
            bounds[span] = (spanStart ?? parentBounds.Start, spanEnd ?? parentBounds.End);
        }

        return true;
    }

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

    private static IReadOnlyList<Artist> ParseArtists(XDocument document, bool strictValidation)
    {
        return document.Descendants()
            .Where(i => strictValidation
                ? i.Name == _ttmNamespace + "agent"
                : i.Name.LocalName == "agent")
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

    private static Dictionary<string, Dictionary<string, List<string>>> ParseITunesTextMap(XDocument document, string containerName, bool strictValidation)
    {
        var result = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.Ordinal);
        foreach (var container in document.Descendants().Where(i => strictValidation
            ? i.Name == _itunesNamespace + containerName
            : i.Name.LocalName.Equals(containerName, StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var text in container.Descendants().Where(i => strictValidation
                ? i.Name == _itunesNamespace + "text"
                : i.Name.LocalName == "text"))
            {
                var key = GetAttributeValue(text, "for");
                if (key is not null && !string.IsNullOrWhiteSpace(text.Value))
                {
                    if (!result.TryGetValue(key, out var translationsByLanguage))
                    {
                        translationsByLanguage = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                        result[key] = translationsByLanguage;
                    }

                    var language = GetInheritedLanguage(text);
                    if (!translationsByLanguage.TryGetValue(language, out var translationTexts))
                    {
                        translationTexts = [];
                        translationsByLanguage[language] = translationTexts;
                    }

                    var translationText = text.Value.Trim(_xmlWhitespaceCharacters);
                    if (!translationTexts.Contains(translationText, StringComparer.Ordinal))
                    {
                        // Conflicting duplicate references must not silently overwrite text.
                        translationTexts.Add(translationText);
                    }
                }
            }
        }

        return result;
    }

    private static Dictionary<string, IReadOnlyList<string>> ParseITunesTransliterations(XDocument document, bool strictValidation)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var transliteration in document.Descendants().Where(i => strictValidation
            ? i.Name == _itunesNamespace + "transliteration"
            : i.Name.LocalName.Equals("transliteration", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var text in transliteration.Elements().Where(i => strictValidation
                ? i.Name == _itunesNamespace + "text"
                : i.Name.LocalName == "text"))
            {
                var key = GetAttributeValue(text, "for");
                var phonetics = text.Elements()
                    .Where(i => strictValidation ? i.Name == _itunesNamespace + "span" : i.Name.LocalName == "span")
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
        IReadOnlyList<string> artistIds,
        bool strictValidation)
    {
        foreach (var span in FindRoleSpans(parent, role, strictValidation))
        {
            var text = NormalizeXmlTextContent(span, ExtractLineText(span, strictValidation));
            if (text.Length == 0)
            {
                continue;
            }

            var language = GetInheritedLanguage(span);
            ReadInheritedTiming(span, parent, start, end, out var spanStart, out var spanEnd);
            var spanArtistIds = GetLineArtistIds(span, strictValidation);
            AddTrackLine(linesByLanguage, language, new LyricLine(text, spanStart)
            {
                End = spanEnd,
                ArtistIds = spanArtistIds.Count > 0 ? spanArtistIds : artistIds
            });
        }
    }

    private static List<LyricSyllable> ParseSyllablesFromChildren(XElement parent, bool strictValidation)
    {
        var timings = new Dictionary<XElement, (long Start, long End)>();
        var timedContainers = new HashSet<XElement>();
        foreach (var span in EnumerateMainSpans(parent, strictValidation))
        {
            ReadTiming(span, out var start, out var end);
            if (!start.HasValue || !end.HasValue)
            {
                continue;
            }

            timings[span] = (start.Value, end.Value);
            for (var ancestor = span.Parent; ancestor is not null && ancestor != parent; ancestor = ancestor.Parent)
            {
                if (!timedContainers.Add(ancestor))
                {
                    break;
                }
            }
        }

        var syllables = new List<LyricSyllable>();
        var leadingText = new StringBuilder();
        XElement? previousTimingSource = null;
        var firstTextPreserved = false;
        var lastTextPreserved = false;
        var leadingTextPreserved = false;
        var pending = new Stack<(XNode Node, XElement? TimingSource)>();
        foreach (var node in parent.Nodes().Reverse())
        {
            pending.Push((node, null));
        }

        // Walk iteratively so deeply nested malformed XML cannot overflow the call stack.
        // Timed containers defer to their timed descendants; auxiliary subtrees never
        // contribute text or timing to the current track.
        while (pending.TryPop(out var entry))
        {
            if (entry.Node is XElement span)
            {
                if (!IsSpan(span, strictValidation) || HasAnyRole(span, "x-translation", "x-bg", "x-roman"))
                {
                    continue;
                }

                var timingSource = timings.ContainsKey(span) && !timedContainers.Contains(span) ? span : entry.TimingSource;
                foreach (var node in span.Nodes().Reverse())
                {
                    pending.Push((node, timingSource));
                }

                continue;
            }

            if (entry.Node is not XText textNode)
            {
                continue;
            }

            var preserve = PreservesXmlSpace(textNode.Parent ?? parent);
            var text = preserve
                ? textNode.Value
                : IsXmlWhitespace(textNode.Value)
                    ? NormalizeInterSyllableSpace(textNode.Value)
                    : WhitespaceCollapseRegex().Replace(textNode.Value, " ");
            if (text.Length == 0)
            {
                continue;
            }

            if (entry.TimingSource is XElement source)
            {
                if (source == previousTimingSource && syllables.Count > 0)
                {
                    syllables[^1].Text += text;
                }
                else
                {
                    var prefix = leadingText.ToString();
                    if (!leadingTextPreserved)
                    {
                        prefix = prefix.TrimStart(_xmlWhitespaceCharacters);
                    }

                    if (syllables.Count == 0)
                    {
                        firstTextPreserved = prefix.Length > 0 ? leadingTextPreserved : preserve;
                    }

                    var timing = timings[source];
                    syllables.Add(new LyricSyllable { Text = prefix + text, Start = timing.Start, End = timing.End });
                    leadingText.Clear();
                    previousTimingSource = source;
                }
            }
            else if (syllables.Count == 0)
            {
                if (leadingText.Length == 0)
                {
                    leadingTextPreserved = preserve;
                }

                leadingText.Append(text);
            }
            else
            {
                // Retain untimed prefixes, suffixes and punctuation in document order.
                syllables[^1].Text += text;
            }

            lastTextPreserved = preserve;
        }

        if (syllables.Count > 0)
        {
            if (!firstTextPreserved)
            {
                syllables[0].Text = syllables[0].Text.TrimStart(_xmlWhitespaceCharacters);
            }

            if (!lastTextPreserved)
            {
                syllables[^1].Text = syllables[^1].Text.TrimEnd(_xmlWhitespaceCharacters);
            }
        }

        return syllables;
    }

    private static string ExtractLineText(XElement element, bool strictValidation)
    {
        var text = new StringBuilder();
        var pending = new Stack<XNode>(element.Nodes().Reverse());
        while (pending.TryPop(out var node))
        {
            switch (node)
            {
                case XText xText:
                    text.Append(xText.Value);
                    break;
                case XElement child when IsSpan(child, strictValidation)
                    && !HasAnyRole(child, "x-translation", "x-bg", "x-roman"):
                    foreach (var childNode in child.Nodes().Reverse())
                    {
                        pending.Push(childNode);
                    }

                    break;
            }
        }

        return text.ToString();
    }

    private static string NormalizeXmlTextContent(XElement element, string text)
    {
        return PreservesXmlSpace(element)
            ? text
            : WhitespaceCollapseRegex().Replace(text, " ").Trim(_xmlWhitespaceCharacters);
    }

    // Keep one word separator between karaoke spans. Drop pure XML indentation
    // (newlines with no space/tab before them); keep a single space when an
    // inline space sits before a pretty-print newline (`</span> \n<span>`).
    private static string NormalizeInterSyllableSpace(string rawText)
    {
        if (string.IsNullOrEmpty(rawText))
        {
            return string.Empty;
        }

        if (!IsXmlWhitespace(rawText))
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

    private static bool IsXmlWhitespace(string text)
        => text.All(i => i is ' ' or '\t' or '\r' or '\n');

    private static IReadOnlyList<string> GetArtistIds(XElement element)
    {
        var agentId = element.Attribute(_ttmNamespace + "agent")?.Value;
        return string.IsNullOrWhiteSpace(agentId) ? [] : [agentId];
    }

    private static IReadOnlyList<string> GetLineArtistIds(XElement element, bool strictValidation)
    {
        var inherited = element.AncestorsAndSelf().Select(GetArtistIds).FirstOrDefault(i => i.Count > 0) ?? [];
        return inherited.Concat(EnumerateMainSpans(element, strictValidation).SelectMany(GetArtistIds))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<XElement> EnumerateMainSpans(XElement parent, bool strictValidation)
    {
        var pending = new Stack<XElement>(parent.Elements().Reverse());
        while (pending.TryPop(out var span))
        {
            if (!IsSpan(span, strictValidation) || HasAnyRole(span, "x-translation", "x-bg", "x-roman"))
            {
                continue;
            }

            yield return span;
            foreach (var child in span.Elements().Reverse())
            {
                pending.Push(child);
            }
        }
    }

    private static IEnumerable<XElement> FindRoleSpans(XElement parent, string role, bool strictValidation)
    {
        var pending = new Stack<XElement>(parent.Elements().Reverse());
        while (pending.TryPop(out var span))
        {
            if (!IsSpan(span, strictValidation))
            {
                continue;
            }

            if (HasRole(span, role))
            {
                yield return span;
            }

            if (HasAnyRole(span, "x-translation", "x-bg", "x-roman"))
            {
                continue;
            }

            foreach (var child in span.Elements().Reverse())
            {
                pending.Push(child);
            }
        }
    }

    private static bool IsSpan(XElement element, bool strictValidation)
        => strictValidation ? element.Name == _ttmlNamespace + "span" : element.Name.LocalName == "span";

    private static bool PreservesXmlSpace(XElement element)
        => element.AncestorsAndSelf().Select(i => i.Attribute(XNamespace.Xml + "space")?.Value)
            .FirstOrDefault(i => i is not null) == "preserve";

    private static string GetInheritedLanguage(XElement element)
        => element.AncestorsAndSelf().Select(i => i.Attribute(XNamespace.Xml + "lang")?.Value)
            .FirstOrDefault(i => i is not null) ?? string.Empty;

    private static void ReadTiming(XElement element, out long? start, out long? end)
    {
        start = ParseTime(GetAttributeValue(element, "begin"));
        end = ParseTime(GetAttributeValue(element, "end"));
        if (start.HasValue && end < start)
        {
            start = null;
            end = null;
        }
    }

    private static void ReadInheritedTiming(XElement element, XElement boundary, long? fallbackStart, long? fallbackEnd, out long? start, out long? end)
    {
        start = null;
        end = null;
        for (var current = element; current is not null && current != boundary; current = current.Parent)
        {
            ReadTiming(current, out var currentStart, out var currentEnd);
            start ??= currentStart;
            end ??= currentEnd;
        }

        start ??= fallbackStart;
        end ??= fallbackEnd;
        if (start.HasValue && end < start)
        {
            start = null;
            end = null;
        }
    }

    private static string? GetAttributeValue(XElement element, string localName)
        => localName switch
        {
            "lang" => element.Attribute(XNamespace.Xml + localName)?.Value,
            "key" or "for" => element.Attribute(localName)?.Value
                ?? element.Attribute(_itunesNamespace + localName)?.Value
                ?? element.Attribute(_itunesInternalNamespace + localName)?.Value,
            _ => GetUnqualifiedAttributeValue(element, localName)
        };

    private static string? GetUnqualifiedAttributeValue(XElement element, string localName)
        => element.Attribute(localName)?.Value;

    private static string? GetNamespacedAttributeValue(XElement element, XNamespace @namespace, string localName)
        => element.Attribute(@namespace + localName)?.Value;

    private static string? GetXmlId(XElement element)
        => element.Attribute(XNamespace.Xml + "id")?.Value;

    private static bool HasRole(XElement element, string role)
        => element.Attribute(_ttmNamespace + "role")?.Value == role;

    private static bool HasAnyRole(XElement element, params string[] roles)
        => roles.Contains(element.Attribute(_ttmNamespace + "role")?.Value, StringComparer.Ordinal);

    private static long? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var match = TtmlTimeRegex().Match(value.Trim());
        if (!match.Success)
        {
            // Apple Music downloads commonly use a partial clock value such as
            // "19.704" or "2:54.285". Keep this compatibility-only; strict
            // validation continues to use ParseAppleTime.
            match = TtmlPartialTimeRegex().Match(value.Trim());
        }

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
                ticks = checked(ticks + long.Parse(paddedFraction, CultureInfo.InvariantCulture));
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
                ticks = checked(ticks + long.Parse(fraction.PadRight(7, '0'), CultureInfo.InvariantCulture));
            }

            return ticks;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or FormatException or OverflowException)
        {
            return null;
        }
    }

    [GeneratedRegex(@"^(?:(?<h>\d+):)?(?<m>\d{1,2}):(?<s>\d{1,2})(?:\.(?<f>\d{1,7}))?$")]
    private static partial Regex TtmlTimeRegex();

    [GeneratedRegex(@"^(?:(?<m>\d{1,2}):)?(?<s>\d{1,2})(?:\.(?<f>\d{1,7}))?$")]
    private static partial Regex TtmlPartialTimeRegex();

    [GeneratedRegex(@"^(?:(?<h>\d+):)?(?<m>\d{2}):(?<s>\d{2})(?:\.(?<f>\d{1,3}))?$")]
    private static partial Regex AppleTimeRegex();

    [GeneratedRegex(@"[ \t\r\n]+")]
    private static partial Regex WhitespaceCollapseRegex();
}
